using System.Text.Json;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps;

public class ExpressStep : AgentStep
{
    public ExpressStep(int stepNumber, string name, string instructions, Gate gate)
        : base(stepNumber, name, "express-relay", instructions, gate) { }

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

    public override string BuildContext(AgentSession? session)
    {
        if (session is null) return "No session context";

        var deliverables = session.Deliverables
            .Select(d => $"- {d.DeliverableId}: {d.Path} ({d.Status})");

        var islandStats = $"Islands: {session.Islands.Count} total, " +
            $"{session.Islands.Count(i => i.Status == IslandStatus.Distilled)} distilled, " +
            $"{session.Islands.Count(i => i.Status == IslandStatus.Discarded)} discarded";

        var questionsBlock = BuildQuestionsBlock(session);

        return $"""
            Deliverables produced:
            {(deliverables.Any() ? string.Join("\n", deliverables) : "None")}

            {islandStats}
            Decisions made: {session.Decisions.Count}

            {questionsBlock}

            Return in the "questions" array:
            - For each answered question: set status to "reviewed" if you incorporated the answer, or "obsolete" if no longer relevant.
            - For each unanswered (open) question: keep status "open".
            - Add any NEW questions discovered during this session with status "open".
            """;
    }

    public override StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied)
    {
        var input = root.TryGetProperty("inputTokens", out var it) ? it.GetInt32() : 0;
        var outputTokens = root.TryGetProperty("outputTokens", out var ot) ? ot.GetInt32() : 0;

        var questions = new List<QuestionRecord>();
        if (root.TryGetProperty("questions", out var qArr))
        {
            foreach (var el in qArr.EnumerateArray())
            {
                var id = el.GetProperty("id").GetString() ?? string.Empty;
                var text = el.GetProperty("text").GetString() ?? string.Empty;
                var status = el.GetProperty("status").GetString() ?? "open";
                questions.Add(new QuestionRecord(id, text, status));
            }
        }

        return new ExpressResult(rawOutput, gateSatisfied, input, outputTokens, questions);
    }

    private static string BuildQuestionsBlock(AgentSession session)
    {
        var questions = session.Questions;
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
    bool GateSatisfied,
    int InputTokens,
    int OutputTokens,
    IReadOnlyList<QuestionRecord> Questions) : StepResult(Output, GateSatisfied)
{
    public override void ApplyTo(AgentSession session)
    {
        session.UpdateTokenConsumption(InputTokens, OutputTokens);

        foreach (var q in Questions)
        {
            var status = Enum.TryParse<QuestionStatus>(q.Status, ignoreCase: true, out var s)
                ? s
                : QuestionStatus.Open;

            var existing = session.FindQuestion(q.Id);
            if (existing is not null)
            {
                session.ApplyQuestionReview(q.Id, status);
            }
            else
            {
                session.RaiseQuestion(q.Id, q.Text, "express-relay");
            }
        }
    }
}

public record QuestionRecord(string Id, string Text, string Status);
