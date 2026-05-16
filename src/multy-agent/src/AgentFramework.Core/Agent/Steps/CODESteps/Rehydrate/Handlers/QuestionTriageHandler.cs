using System.Text.Json;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.Rehydrate.Handlers;

internal sealed class QuestionTriageHandler(IMarkFileReader reader) : IRehydrateContextHandler
{
    private const string SystemPrompt =
        "You are triaging open questions from a prior agent session. " +
        "Based on the iteration context provided, classify each question. " +
        "Respond with valid JSON only.";

    private const string TriageSchema = """
        {
          "triaged": [
            { "id": "Q-001", "status": "resolved|obsolete|still_open",
              "answer": "string or null", "blockerSeverity": "hard|soft|null" }
          ]
        }
        """;

    public async Task<HandlerExchange> HandleAsync(
        HandlerExchange? previousExchange,
        IAgentRunContext? context, ISessionWriter writer,
        IChatClient chatClient, CancellationToken ct)
    {
        if (context is null)
            return Exchange(previousExchange, "No context — skipping question triage.", isLlmCall: false);

        var questionsMd = await reader.ReadQuestionsLogAsync(ct);
        if (questionsMd is not null)
            RestoreQuestionsFromMark(questionsMd, writer);

        var openQuestions = context.Questions.Where(q => q.Status == QuestionStatus.Open).ToList();

        if (openQuestions.Count == 0)
            return Exchange(previousExchange, "No open questions — proceeding without question triage.", isLlmCall: false);

        var questionMap = openQuestions.ToDictionary(q => q.Id, q => q.Text);
        var messages = BuildMessages(previousExchange?.Content.Output ?? string.Empty, openQuestions);

        var triage = await chatClient.SendHandlerAsync(
            messages,
            TriageSchema,
            root => ParseTriageResponse(root, questionMap),
            ct);

        ApplyTriage(triage, writer);

        var resolved = triage.Triaged.Count(q => q.Status == "resolved");
        var obsolete = triage.Triaged.Count(q => q.Status == "obsolete");
        var stillOpen = triage.Triaged.Count(q => q.Status == "still_open");

        var blockerLines = triage.Blockers.Count > 0
            ? "\nBlockers:\n" + string.Join('\n', triage.Blockers.Select(b => $"  - {b.Id} [{b.BlockerSeverity ?? "soft"}]: {b.Text}"))
            : string.Empty;

        var output = $"Triage complete: {resolved} resolved, {obsolete} obsolete, {stillOpen} still open.{blockerLines}";
        return Exchange(previousExchange, output, isLlmCall: true);
    }

    private HandlerExchange Exchange(HandlerExchange? previous, string output, bool isLlmCall) =>
        new(GetType().Name,
            new HandlerContent(previous?.Content.Output ?? "[start]", output),
            new HandlerMetadata(previous?.Sender, IsLlmCall: isLlmCall));

    private static IReadOnlyList<ChatMessage> BuildMessages(
        string iterationContext, IReadOnlyList<Question> openQuestions)
    {
        var questionLines = string.Join('\n',
            openQuestions.Select(q => $"  - {q.Id}: {q.Text}"));

        var userContent = $"""
            {iterationContext}

            Open questions requiring triage:
            {questionLines}

            For each question:
              - If the session context resolves it → status: "resolved", provide the answer
              - If no longer relevant → status: "obsolete"
              - Otherwise → status: "still_open", classify severity as "hard" or "soft"
            """;

        return
        [
            new ChatMessage(MessageRole.System, SystemPrompt),
            new ChatMessage(MessageRole.User, userContent)
        ];
    }

    private static void RestoreQuestionsFromMark(string questionsMd, ISessionWriter writer)
    {
        if (!MarkSectionParser.SectionIsEmpty(questionsMd, "Active (OPEN)"))
        {
            var rows = MarkSectionParser.ExtractTableRows(questionsMd, "Active (OPEN)");
            foreach (var row in rows.Where(r => r.Length >= 2))
                writer.RestoreQuestion(row[0], row[1], "questions-log", QuestionStatus.Open);
        }

        var resolvedRows = MarkSectionParser.ExtractTableRows(questionsMd, "Resolved");
        foreach (var row in resolvedRows.Where(r => r.Length >= 4))
            writer.RestoreQuestion(row[0], row[1], "questions-log",
                QuestionStatus.Reviewed, row[2], row[3]);

        var obsoleteRows = MarkSectionParser.ExtractTableRows(questionsMd, "Obsolete");
        foreach (var row in obsoleteRows.Where(r => r.Length >= 2))
            writer.RestoreQuestion(row[0], row[1], "questions-log", QuestionStatus.Obsolete);
    }

    private static QuestionTriageContext ParseTriageResponse(
        JsonElement root, Dictionary<string, string> questionMap)
    {
        if (!root.TryGetProperty("triaged", out var arr)) return QuestionTriageContext.Empty;

        var triaged = new List<TriagedQuestion>();
        foreach (var el in arr.EnumerateArray())
        {
            var id = el.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
            var text = questionMap.TryGetValue(id, out var t) ? t : "";
            var status = el.TryGetProperty("status", out var stEl) ? stEl.GetString() ?? "still_open" : "still_open";
            var answer = el.TryGetProperty("answer", out var ansEl) && ansEl.ValueKind != JsonValueKind.Null
                ? ansEl.GetString() : null;
            var severity = el.TryGetProperty("blockerSeverity", out var sevEl) && sevEl.ValueKind != JsonValueKind.Null
                ? sevEl.GetString() : null;

            triaged.Add(new TriagedQuestion(id, text, status, answer, severity));
        }

        return new QuestionTriageContext(triaged);
    }

    private static void ApplyTriage(QuestionTriageContext triage, ISessionWriter writer)
    {
        foreach (var triaged in triage.Triaged)
        {
            switch (triaged.Status)
            {
                case "resolved":
                    writer.AnswerQuestion(triaged.Id, triaged.Answer ?? "resolved in session", "session-triage");
                    break;
                case "obsolete":
                    writer.ReviewQuestion(triaged.Id, QuestionStatus.Obsolete);
                    break;
            }
        }
    }

    private sealed record QuestionTriageContext(IReadOnlyList<TriagedQuestion> Triaged)
    {
        public static readonly QuestionTriageContext Empty = new([]);

        public IReadOnlyList<TriagedQuestion> Blockers =>
            Triaged.Where(q => q.Status == "still_open").ToList().AsReadOnly();
    }

    private sealed record TriagedQuestion(
        string Id,
        string Text,
        string Status,
        string? Answer = null,
        string? BlockerSeverity = null);
}
