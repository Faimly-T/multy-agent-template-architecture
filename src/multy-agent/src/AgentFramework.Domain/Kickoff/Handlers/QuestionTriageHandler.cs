using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.Kickoff.Handlers;

internal sealed class QuestionTriageHandler(
    IStepPromptLayer? stepCtx = null)
    : CommandHandlerBase(
        stepCtx, 
        "You are triaging open questions from a prior agent session. " +
        "Based on the distill session context provided, classify each question " +
        "and identify which have been resolved by new information. " +
        "Respond with valid JSON only.")
{
    private const string TriageSchema = """
        {
          "triaged": [
            { "id": "Q-001", "status": "resolved|obsolete|still_open",
              "answer": "string or null", "blockerSeverity": "hard|soft|null" }
          ]
        }
        """;

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext? agentContext,
        ISessionWriter    writer,
        IChatClient?      chatClient,
        CancellationToken ct)
    {
        if (agentContext is null)
            return Exchange(context, "No context — skipping question triage.", isLlmCall: false);

        var allQuestions  = agentContext.Questions;
        var openQuestions = allQuestions.Where(q => q.Status == QuestionStatus.Open).ToList();
        var answeredQuestions = allQuestions
            .Where(q => q.Status is QuestionStatus.Answered or QuestionStatus.Reviewed)
            .ToList();

        if (openQuestions.Count == 0 && answeredQuestions.Count == 0)
            return Exchange(context, "No questions on record — proceeding without question triage.", isLlmCall: false);

        if (openQuestions.Count == 0)
        {
            var answeredSummary = BuildAnsweredSummary(answeredQuestions);
            return Exchange(context, $"No open questions to triage.\n\n{answeredSummary}", isLlmCall: false);
        }

        var questionMap = openQuestions.ToDictionary(q => q.Id, q => q.Text);
        var userContent = BuildTriageUserContent(
            context.LastOutput() ?? string.Empty,
            answeredQuestions,
            openQuestions);
        var messages = BuildPrompt(userContent);

        var triage = await chatClient!.SendHandlerAsync(
            messages,
            TriageSchema,
            root => ParseTriageResponse(root, questionMap),
            ct);

        ApplyTriage(triage, writer);

        var resolvedItems = triage.Triaged.Where(q => q.Status == "resolved").ToList();
        var obsoleteCount = triage.Triaged.Count(q => q.Status == "obsolete");
        var stillOpen     = triage.Triaged.Count(q => q.Status == "still_open");

        // Surfaces all resolved knowledge (prior + just-triaged) so ObjectiveSynthesis
        // can focus the new session objective on what was unblocked.
        var lines = new List<string>
        {
            $"Question triage: {resolvedItems.Count} resolved, {obsoleteCount} obsolete, {stillOpen} still open."
        };

        if (answeredQuestions.Count > 0)
        {
            lines.Add("\nPreviously answered questions (context):");
            foreach (var q in answeredQuestions)
                lines.Add($"  [{q.Id}] {q.Text}\n  → {q.Answer}");
        }

        if (resolvedItems.Count > 0)
        {
            lines.Add("\nNewly resolved — these answers should shape the new session focus:");
            foreach (var q in resolvedItems)
                lines.Add($"  [{q.Id}] {q.Text}\n  → Answer: {q.Answer}");
        }

        if (triage.Blockers.Count > 0)
        {
            lines.Add("\nStill-open blockers:");
            foreach (var b in triage.Blockers)
                lines.Add($"  [{b.Id}] [{b.BlockerSeverity ?? "soft"}] {b.Text}");
        }

        return Exchange(context, string.Join('\n', lines), isLlmCall: true);
    }

    private static string BuildAnsweredSummary(IReadOnlyList<Question> answered)
    {
        var lines = answered.Select(q => $"  [{q.Id}] {q.Text}\n  → {q.Answer}");
        return "Previously answered questions:\n" + string.Join('\n', lines);
    }

    private HandlerExchange Exchange(IReadOnlyList<HandlerExchange> context, string output, bool isLlmCall) =>
        new(GetType().Name,
            new HandlerContent(context.LastOutput() ?? "[start]", output),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: isLlmCall));

    private static string BuildTriageUserContent(
        string distillSessionContext,
        IReadOnlyList<Question> answeredQuestions,
        IReadOnlyList<Question> openQuestions)
    {
        var answeredSection = answeredQuestions.Count > 0
            ? "Previously answered questions (for context — do NOT re-triage these):\n" +
              string.Join('\n', answeredQuestions.Select(q =>
                  $"  [{q.Id}] {q.Text}\n  → Answer: {q.Answer}"))
            : string.Empty;

        var openLines = string.Join('\n',
            openQuestions.Select(q => $"  [{q.Id}] {q.Text}"));

        return $"""
            Previous distill session context:
            {distillSessionContext}

            {answeredSection}

            Open questions to triage against this context:
            {openLines}

            For each open question only:
              - If the distill context resolves it → status: "resolved", provide a concise answer
              - If no longer relevant to the new session direction → status: "obsolete"
              - Otherwise → status: "still_open", classify blocking severity as "hard" or "soft"
            """;
    }

    private static QuestionTriageContext ParseTriageResponse(
        JsonElement root, Dictionary<string, string> questionMap)
    {
        if (!root.TryGetProperty("triaged", out var arr)) return QuestionTriageContext.Empty;

        var triaged = new List<TriagedQuestion>();
        foreach (var el in arr.EnumerateArray())
        {
            var id       = el.TryGetProperty("id",              out var idEl)  ? idEl.GetString()  ?? "" : "";
            var text     = questionMap.TryGetValue(id, out var t) ? t : "";
            var status   = el.TryGetProperty("status",          out var stEl)  ? stEl.GetString()  ?? "still_open" : "still_open";
            var answer   = el.TryGetProperty("answer",          out var ansEl) && ansEl.ValueKind != JsonValueKind.Null ? ansEl.GetString() : null;
            var severity = el.TryGetProperty("blockerSeverity", out var sevEl) && sevEl.ValueKind != JsonValueKind.Null ? sevEl.GetString() : null;

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
        string  Id,
        string  Text,
        string  Status,
        string? Answer          = null,
        string? BlockerSeverity = null);
}
