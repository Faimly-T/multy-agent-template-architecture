using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.UxAgent.Express.Handlers;

/// <summary>
/// Handler 6 of 6 in the UxExpress chain — the FINAL handler.
/// Reviews all session questions and determines their new status after the Express run.
/// Also self-reports token consumption estimates.
/// This handler's output is the FinalJson used by UxExpressStep.ParseResult.
/// </summary>
internal sealed class UxQuestionReviewHandler : CommandHandlerBase
{
    internal const string ReviewSchema = """
        {
          "questions": [
            {
              "id": "Q-001",
              "text": "string — question text (copy from context, or new question text)",
              "status": "open | reviewed | obsolete"
            }
          ],
          "inputTokens": 0,
          "outputTokens": 0,
          "gateSatisfied": true
        }
        """;

    private const string Instruction =
        "You are the session relay agent. Review all session questions and update their statuses. " +
        "A question is 'reviewed' if its answer was incorporated into any document generated this session. " +
        "A question is 'obsolete' if it is no longer relevant given what was produced. " +
        "A question stays 'open' if it still needs an external answer. " +
        "You may also add NEW questions discovered during this express session. " +
        "Set gateSatisfied=true when all four documents have been generated (summary, persona, research, interview). " +
        "Report approximate token usage: inputTokens and outputTokens as integers.";

    public UxQuestionReviewHandler(IStepPromptLayer? stepCtx = null)
        : base(stepCtx, Instruction) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext?  agentContext,
        ISessionWriter     writer,
        IChatClient?       chatClient,
        CancellationToken  ct)
    {
        var questions   = agentContext?.Questions ?? [];
        var deliverables = agentContext?.Deliverables ?? [];

        // Summarise which documents were produced in this run
        var documentsProduced = new List<string>();
        if (context.Any(e => e.Sender == nameof(UxSessionSummaryHandler)))
            documentsProduced.Add("Session Summary HTML");
        if (context.Any(e => e.Sender == nameof(BuyerPersonaDocumentHandler)))
            documentsProduced.Add("Buyer Persona Document HTML");
        if (context.Any(e => e.Sender == nameof(ResearchValidationGuideHandler)))
            documentsProduced.Add("Research Validation Guide HTML");
        if (context.Any(e => e.Sender == nameof(PersonaInterviewScriptHandler)))
            documentsProduced.Add("Persona Interview Script HTML");

        var questionsBlock = questions.Any()
            ? string.Join("\n", questions.Select(q =>
            {
                var answerPart = q.Answer is not null
                    ? $" → Answer: {q.Answer} (Source: {q.AnswerSource})"
                    : string.Empty;
                return $"  [{q.Id}] [{q.Status}]: {q.Text}{answerPart}";
            }))
            : "  No questions logged this session.";

        var userContent = $"""
            Documents produced in this Express run:
            {string.Join("\n", documentsProduced.Select(d => $"  ✓ {d}"))}

            Deliverables from Distill step:
            {(deliverables.Any() ? string.Join("\n", deliverables.Select(d => $"  - {d.DeliverableId}: {d.Path}")) : "  None")}

            Current questions to review:
            {questionsBlock}

            For each question:
            - "reviewed": the answer was incorporated into one of the documents above
            - "obsolete": the question is no longer relevant given the research produced
            - "open": still needs an external answer (human, another agent, or future research)
            Add any NEW open questions discovered during this Express session.

            Set gateSatisfied=true because {documentsProduced.Count} of 4 documents were produced.
            Estimate inputTokens and outputTokens based on this session's work.

            Respond using EXACTLY this JSON structure — use these field names verbatim:
            {ReviewSchema}
            """;

        var messages = BuildPrompt(userContent, skillNames: ["express-relay"]);
        var json = await chatClient!.SendHandlerAsync(
            messages, ReviewSchema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(userContent, json),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }
}
