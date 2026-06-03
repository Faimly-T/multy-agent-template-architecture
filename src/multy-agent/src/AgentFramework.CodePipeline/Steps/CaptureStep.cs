using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.CodePipeline;

public class CaptureStep : AgentStep
{
    public CaptureStep(
        IStepPromptLayer       stepContext,
        int                    stepNumber,
        string                 instructions,
        Gate                   gate,
        IStepChain?            chain             = null,
        Func<string, string?>? instructionLookup = null)
        : base(stepContext, stepNumber, instructions, gate, chain, instructionLookup) { }

    /// <summary>Gate: at least 3 islands must be captured.</summary>
    protected override bool EvaluateGate(JsonElement root, IReadOnlyList<HandlerExchange> journal)
    {
        if (!root.TryGetProperty("islands", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return false;
        return arr.GetArrayLength() >= 3;
    }

    public override string JsonResponseSchema => """
        {
          "islands": [
            {
              "id": "ISL-XXX",
              "type": "UserType | Goal | PainPoint | BehavioralPattern | ContextOfUse | EmotionalState | AntiUser | Stakeholder | AccessibilitySignal",
              "description": "string",
              "source": "string",
              "relatesToIslandId": "ISL-XXX or null"
            }
          ],
          "gateSatisfied": true
        }
        """;

    public override string BuildContext(IAgentRunContext? context)
    {
        var objective = context?.Session?.CurrentCheckpoint?.SessionObjective ?? "No objective";
        return $"Session Objective: {objective}";
    }

    public override StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied)
    {
        var islands = new List<CapturedIsland>();
        if (root.TryGetProperty("islands", out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var el in arr.EnumerateArray())
            {
                var id      = el.TryGetProperty("id",          out var idEl)   ? idEl.GetString()   ?? string.Empty : string.Empty;
                var typeStr = el.TryGetProperty("type",        out var typeEl) ? typeEl.GetString()  ?? "Goal"       : "Goal";
                var type    = Enum.TryParse<IslandType>(typeStr, ignoreCase: true, out var t) ? t : IslandType.Goal;
                var desc    = el.TryGetProperty("description", out var descEl) ? descEl.GetString()  ?? string.Empty : string.Empty;
                var source  = el.TryGetProperty("source",      out var srcEl)  ? srcEl.GetString()   ?? string.Empty : string.Empty;
                string? relatesTo = el.TryGetProperty("relatesToIslandId", out var rel)
                    && rel.ValueKind != JsonValueKind.Null ? rel.GetString() : null;

                if (!string.IsNullOrWhiteSpace(id))
                    islands.Add(new CapturedIsland(id, type, desc, source, relatesTo));
            }
        return new CaptureResult(rawOutput, gateSatisfied, islands);
    }
}

public record CaptureResult(
    string Output,
    bool   GateSatisfied,
    IReadOnlyList<CapturedIsland> Islands) : StepResult(Output, GateSatisfied)
{
    public override void ApplyTo(ISessionWriter writer)
    {
        writer.SetCapturedIslands(Islands);
    }
}
