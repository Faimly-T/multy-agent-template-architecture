using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.CodePipeline;

public class KickoffStep : AgentStep
{
    public KickoffStep(
        IStepPromptLayer stepContext,
        int              stepNumber,
        string           instructions,
        Gate             gate,
        IStepChain?      chain = null)
        : base(stepContext, stepNumber, instructions, gate, chain) { }

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

        var blockers = new List<KickoffBlocker>();
        if (root.TryGetProperty("blockers", out var bArr))
            foreach (var el in bArr.EnumerateArray())
            {
                var qId      = el.GetProperty("questionId").GetString() ?? string.Empty;
                var text     = el.GetProperty("text").GetString() ?? string.Empty;
                var severity = el.GetProperty("severity").GetString() ?? "soft";
                blockers.Add(new KickoffBlocker(qId, text, severity));
            }

        return new KickoffResult(rawOutput, gateSatisfied, objective, narrativeBridge,
            stalenessWarning, isInitialSession, blockers);
    }
}

public record KickoffBlocker(string QuestionId, string Text, string Severity);

public record KickoffResult(
    string Output,
    bool   GateSatisfied,
    string SessionObjective,
    string NarrativeBridge     = "",
    string? StalenessWarning   = null,
    bool   IsInitialSession    = false,
    IReadOnlyList<KickoffBlocker>? Blockers = null) : StepResult(Output, GateSatisfied)
{
    public override void ApplyTo(ISessionWriter writer)
    {
        writer.BeginIteration(SessionObjective);
    }
}
