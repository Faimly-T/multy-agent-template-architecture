using AgentFramework.CodePipeline;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Domain.UxAgent.Express;

namespace AgentFramework.Domain.UxAgent;

/// <summary>
/// IDeliverableWriter that publishes the completed session results to a message bus.
/// Implements the "forward questions to a message bus" pattern described in the architecture.
///
/// Published topics:
///   "ux.session.completed"  — full session context (objective, decisions, deliverables)
///   "ux.questions.open"     — any open questions forwarded to the next agent or human inbox
///   "ux.express.deliverable"— each HTML document published separately for downstream consumers
///
/// Inject IMessagePublisher from DI — use MockMessagePublisher in tests/dev,
/// a real broker implementation (Azure Service Bus, RabbitMQ, etc.) in production.
/// </summary>
public sealed class ServiceBusDeliverableWriter : IDeliverableWriter
{
    private readonly IMessagePublisher _publisher;

    public ServiceBusDeliverableWriter(IMessagePublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public async Task WriteAsync(
        IAgentRunContext          context,
        IReadOnlyList<StepResult> results,
        CancellationToken         ct = default)
    {
        var expressResult = results.OfType<UxExpressResult>().LastOrDefault();
        var closedResult  = results.OfType<ClosedResult>().LastOrDefault();

        var checkpoint   = context.Session?.CurrentCheckpoint;
        var projectId    = context.Session?.ProjectId ?? "unknown";
        var sessionClosed = closedResult?.ClosedAt ?? DateTime.UtcNow;

        // ── 1. Publish full session summary ──────────────────────────────
        var sessionPayload = new SessionCompletedMessage(
            ProjectId:       projectId,
            SessionObjective: checkpoint?.SessionObjective ?? string.Empty,
            ClosedAt:         sessionClosed,
            TotalInputTokens:  closedResult?.TotalInputTokens  ?? checkpoint?.TokensConsumption.InputTokens  ?? 0,
            TotalOutputTokens: closedResult?.TotalOutputTokens ?? checkpoint?.TokensConsumption.OutputTokens ?? 0,
            IsValid:          closedResult?.GateSatisfied ?? true,
            ValidationSummary: closedResult?.Validation.Summary ?? string.Empty,
            DecisionCount:    context.Decisions.Count,
            DeliverableCount: context.Deliverables.Count,
            OpenQuestionCount: context.Questions.Count(q => q.Status == QuestionStatus.Open));

        await _publisher.PublishAsync("ux.session.completed", sessionPayload, ct);

        // ── 2. Forward open questions ─────────────────────────────────────
        var openQuestions = context.Questions.Where(q => q.Status == QuestionStatus.Open).ToList();
        if (openQuestions.Any())
        {
            var questionsPayload = new OpenQuestionsMessage(
                ProjectId:  projectId,
                ClosedAt:   sessionClosed,
                Questions:  openQuestions.Select(q => new QuestionSummary(q.Id, q.Text)).ToList().AsReadOnly());

            await _publisher.PublishAsync("ux.questions.open", questionsPayload, ct);
        }

        // ── 3. Publish individual HTML deliverables ───────────────────────
        if (expressResult is not null)
        {
            var documents = new[]
            {
                ("session-summary",          expressResult.HtmlSummary),
                ("buyer-persona",            expressResult.HtmlPersona),
                ("research-validation-guide",expressResult.HtmlResearch),
                ("interview-script",         expressResult.HtmlInterview),
                ("next-session-guide",       expressResult.HtmlNextSession),
            };

            foreach (var (slug, html) in documents)
            {
                if (string.IsNullOrWhiteSpace(html)) continue;

                await _publisher.PublishAsync(
                    "ux.express.deliverable",
                    new DeliverableMessage(projectId, slug, html, sessionClosed),
                    ct);
            }
        }
    }
}

// ── Message payload types ────────────────────────────────────────────────────

public sealed record SessionCompletedMessage(
    string   ProjectId,
    string   SessionObjective,
    DateTime ClosedAt,
    int      TotalInputTokens,
    int      TotalOutputTokens,
    bool     IsValid,
    string   ValidationSummary,
    int      DecisionCount,
    int      DeliverableCount,
    int      OpenQuestionCount);

public sealed record OpenQuestionsMessage(
    string                      ProjectId,
    DateTime                    ClosedAt,
    IReadOnlyList<QuestionSummary> Questions);

public sealed record QuestionSummary(string Id, string Text);

public sealed record DeliverableMessage(
    string   ProjectId,
    string   DocumentSlug,
    string   HtmlContent,
    DateTime PublishedAt);
