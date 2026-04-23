using System.Text.Json;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps;

public class RehydrateStep : AgentStep
{

    public RehydrateStep(int stepNumber, string name, string instructions, Gate gate)
        : base(stepNumber, name, "rehydrate-context", instructions, gate) { }

    public override string JsonResponseSchema => """
        {
          "sessionObjective": "string — verb + deliverable + success condition + stakes clause",
          "narrativeBridge": "string — 2-3 sentences connecting last session's end to this session's start",
          "isInitialSession": false,
          "stalenessWarning": "string or null — staleness warning if checkpoint is stale",
          "blockers": [
            { "questionId": "Q-001", "text": "blocker description", "severity": "hard|soft" }
          ],
          "gateSatisfied": true
        }
        """;

    public override string BuildContext(AgentSession? session)
    {
        if (session is null)
            return "Initial Session — no prior context exists. This is session #1.";

        var isInitial = session.Checkpoint.SessionIteration <= 1;

        // --- Header: session type ---
        string context;
        if (isInitial)
        {
            context = $"""
                Initial Session — session #1. No prior session state.
                User input objective: {session.Checkpoint.SessionObjective}

                Synthesize a Session Objective containing: verb + deliverable + success condition + stakes clause.
                """;
        }
        else
        {
            context = $"""
                Session iteration: {session.Checkpoint.SessionIteration}
                Prior checkpoint date: {session.Checkpoint.Date:yyyy-MM-dd}
                Prior session objective: {session.Checkpoint.SessionObjective}
                """;

            // Staleness check
            var daysSinceCheckpoint = (DateTime.UtcNow - session.Checkpoint.Date).Days;
            if (daysSinceCheckpoint > 3)
            {
                context += $"\n\n⚠️ Staleness warning: Last checkpoint was {daysSinceCheckpoint} days ago — priorities may have shifted.";
            }
        }

        // --- Answered questions (always included, carry through all steps) ---
        var answeredQuestions = session.Questions
            .Where(q => q.Status == QuestionStatus.Answered)
            .ToList();

        if (answeredQuestions.Count > 0)
        {
            var lines = answeredQuestions.Select(q =>
                $"- {q.Id}: {q.Text} → Answer: {q.Answer} (Source: {q.AnswerSource})");

            context += $"""


                Answered questions from prior session (use these throughout all steps):
                {string.Join("\n", lines)}

                In the Express step, return a reviewed list confirming which answers you've incorporated.
                """;
        }

        // --- Open questions (triage each) ---
        var openQuestions = session.Questions
            .Where(q => q.Status == QuestionStatus.Open)
            .ToList();

        if (openQuestions.Count > 0)
        {
            var lines = openQuestions.Select(q => $"- {q.Id}: {q.Text}");

            context += $"""


                Open questions from prior session (triage each — resolve, obsolete, or flag as blocker):
                {string.Join("\n", lines)}
                """;
        }

        // --- Prior work summary ---
        if (session.Backlog.Count > 0 || session.Decisions.Count > 0 || session.Deliverables.Count > 0)
        {
            context += $"""


                Prior work summary:
                Islands: {session.Backlog.Count} total ({session.Backlog.GetByStatus(IslandStatus.Distilled).Count} distilled)
                Decisions: {session.Decisions.Count}
                Deliverables: {session.Deliverables.Count}
                """;
        }

        return context;
    }

    public override StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied)
    {
        var objective = root.GetProperty("sessionObjective").GetString() ?? string.Empty;

        var narrativeBridge = root.TryGetProperty("narrativeBridge", out var nb)
            ? nb.GetString() ?? string.Empty
            : string.Empty;

        var isInitialSession = root.TryGetProperty("isInitialSession", out var init)
            && init.GetBoolean();

        var stalenessWarning = root.TryGetProperty("stalenessWarning", out var sw)
            ? sw.GetString()
            : null;

        var blockers = new List<RehydrateBlocker>();
        if (root.TryGetProperty("blockers", out var bArr))
        {
            foreach (var el in bArr.EnumerateArray())
            {
                var qId = el.GetProperty("questionId").GetString() ?? string.Empty;
                var text = el.GetProperty("text").GetString() ?? string.Empty;
                var severity = el.GetProperty("severity").GetString() ?? "soft";
                blockers.Add(new RehydrateBlocker(qId, text, severity));
            }
        }

        return new RehydrateResult(rawOutput, gateSatisfied, objective, narrativeBridge, stalenessWarning, isInitialSession, blockers);
    }
}

public record RehydrateBlocker(string QuestionId, string Text, string Severity);

public record RehydrateResult(
    string Output,
    bool GateSatisfied,
    string SessionObjective,
    string NarrativeBridge = "",
    string? StalenessWarning = null,
    bool IsInitialSession = false,
    IReadOnlyList<RehydrateBlocker>? Blockers = null) : StepResult(Output, GateSatisfied)
{
    public override void ApplyTo(ISessionWriter writer)
    {
        writer.UpdateObjective(SessionObjective);
    }
}
