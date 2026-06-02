using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.CodePipeline;

/// <summary>
/// Step 5 — Express.
///
/// Chain execution, journal recording, and gate evaluation are handled by the base
/// <see cref="AgentStep.ExecuteStepAsync"/> template. Subclasses can override:
/// <list type="bullet">
///   <item><see cref="AgentStep.EvaluateGate"/> — base reads JSON <c>gateSatisfied</c>;
///     override to check artifact presence (e.g. <c>UxExpressStep</c>).</item>
///   <item><see cref="AgentStep.BuildChainResult"/> — base calls <see cref="ParseResult"/>;
///     override to return a richer type carrying extracted HTML documents.</item>
/// </list>
/// </summary>
public class ExpressStep : AgentStep
{
    public ExpressStep(
        IStepPromptLayer stepContext,
        int              stepNumber,
        string           instructions,
        Gate             gate,
        IStepChain?      chain = null)
        : base(stepContext, stepNumber, instructions, gate, chain) { }

    public override string JsonResponseSchema => """
        {
          "inputTokens": 0,
          "outputTokens": 0,
          "questions": [
            { "id": "Q-001", "text": "question text", "status": "open|reviewed|obsolete" }
          ],
          "gateSatisfied": true
        }
        """;

    public override string BuildContext(IAgentRunContext? context)
    {
        if (context is null) return "No session context";

        var deliverables = context.Deliverables
            .Select(d => $"- {d.DeliverableId}: {d.Path} ({d.Status})");

        var backlog     = context.Session?.Backlog;
        var islandStats = backlog is not null
            ? $"Islands: {backlog.Count} total, " +
              $"{backlog.GetByStatus(IslandStatus.Distilled).Count} distilled, " +
              $"{backlog.GetByStatus(IslandStatus.Discarded).Count} discarded"
            : "Islands: none";

        var questionsBlock = BuildQuestionsBlock(context.Questions);

        return $"""
            Deliverables produced:
            {(deliverables.Any() ? string.Join("\n", deliverables) : "None")}

            {islandStats}
            Decisions made: {context.Decisions.Count}

            {questionsBlock}

            Return in the "questions" array:
            - For each answered question: set status to "reviewed" if you incorporated the answer, or "obsolete" if no longer relevant.
            - For each unanswered (open) question: keep status "open".
            - Add any NEW questions discovered during this session with status "open".
            """;
    }

    public override StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied)
    {
        var input        = root.TryGetProperty("inputTokens",  out var it) ? it.GetInt32() : 0;
        var outputTokens = root.TryGetProperty("outputTokens", out var ot) ? ot.GetInt32() : 0;

        var questions = new List<QuestionRecord>();
        if (root.TryGetProperty("questions", out var qArr))
            foreach (var el in qArr.EnumerateArray())
            {
                var id     = el.GetProperty("id").GetString()     ?? string.Empty;
                var text   = el.GetProperty("text").GetString()   ?? string.Empty;
                var status = el.GetProperty("status").GetString() ?? "open";
                questions.Add(new QuestionRecord(id, text, status));
            }

        return new ExpressResult(rawOutput, gateSatisfied, input, outputTokens, questions);
    }

    private static string BuildQuestionsBlock(IReadOnlyList<Question> questions)
    {
        if (questions.Count == 0) return "No questions logged.";

        var lines = questions.Select(q =>
        {
            var answerPart = q.Answer is not null
                ? $" → Answer: {q.Answer} (Source: {q.AnswerSource})"
                : string.Empty;
            return $"- {q.Id} [{q.Status}]: {q.Text}{answerPart}";
        });

        return $"""
            Current questions:
            {string.Join("\n", lines)}
            """;
    }
}

public record ExpressResult(
    string Output,
    bool   GateSatisfied,
    int    InputTokens,
    int    OutputTokens,
    IReadOnlyList<QuestionRecord> Questions) : StepResult(Output, GateSatisfied)
{
    public override void ApplyTo(ISessionWriter writer)
    {
        writer.UpdateTokenConsumption(InputTokens, OutputTokens);

        foreach (var q in Questions)
        {
            var status = Enum.TryParse<QuestionStatus>(q.Status, ignoreCase: true, out var s)
                ? s : QuestionStatus.Open;
            writer.ReviewQuestion(q.Id, status, q.Text);
        }
    }
}

public record QuestionRecord(string Id, string Text, string Status);
