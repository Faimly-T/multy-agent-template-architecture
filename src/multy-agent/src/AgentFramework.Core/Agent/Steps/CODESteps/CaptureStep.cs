using System.Text.Json;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps;

public class CaptureStep : AgentStep
{
    public CaptureStep(int stepNumber, string name, string instructions, Gate gate)
        : base(stepNumber, name, "autonomous-capture", instructions, gate) { }

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

    public override string BuildContext(AgentSession? session)
    {
        var objective = session?.Checkpoint.SessionObjective ?? "No objective";
        return $"Session Objective: {objective}";
    }

    public override StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied)
    {
        var islands = new List<CapturedIsland>();
        if (root.TryGetProperty("islands", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var id = el.GetProperty("id").GetString() ?? string.Empty;
                var typeStr = el.GetProperty("type").GetString() ?? "Goal";
                var type = Enum.TryParse<IslandType>(typeStr, ignoreCase: true, out var t) ? t : IslandType.Goal;
                var desc = el.GetProperty("description").GetString() ?? string.Empty;
                var source = el.GetProperty("source").GetString() ?? string.Empty;
                string? relatesTo = el.TryGetProperty("relatesToIslandId", out var rel) && rel.ValueKind != JsonValueKind.Null
                    ? rel.GetString()
                    : null;

                islands.Add(new CapturedIsland(id, type, desc, source, relatesTo));
            }
        }
        return new CaptureResult(rawOutput, gateSatisfied, islands);
    }
}

public record CaptureResult(
    string Output,
    bool GateSatisfied,
    IReadOnlyList<CapturedIsland> Islands) : StepResult(Output, GateSatisfied)
{
    public override void ApplyTo(AgentSession session)
    {
        session.Backlog.SetCaptured(Islands);
    }
}

public record CapturedIsland(
    string Id,
    IslandType Type,
    string Description,
    string Source,
    string? RelatesToIslandId = null);
