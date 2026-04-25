using System.Text.Json;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps;

public class OrganizeStep : AgentStep
{
    public OrganizeStep(int stepNumber, string name, string instructions, Gate gate)
        : base(stepNumber, name, "strategic-organize", instructions, gate) { }

    public override string JsonResponseSchema => """
        {
          "organizedIslands": [
            {
              "islandId": "ISL-XXX",
              "newStatus": "Organized | Discarded"
            }
          ],
          "decisions": [
            {
              "id": "DEC-XXX",
              "description": "string",
              "impact": "string"
            }
          ],
          "gateSatisfied": true
        }
        """;

    public override string BuildContext(AgentSession? session)
    {
        if (session is null || session.Backlog.Count == 0)
            return "Current Islands: None captured yet";

        var lines = session.Backlog.All.Select(i =>
            $"- {i.Id} [{i.Type}] {i.Description} (Status: {i.Status})" +
            (i.RelatesToIslandId is not null ? $" → relates to {i.RelatesToIslandId}" : ""));
        return $"Current Islands:\n{string.Join("\n", lines)}";
    }

    public override StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied)
    {
        var organized = new List<IslandOrganization>();
        if (root.TryGetProperty("organizedIslands", out var oi))
        {
            foreach (var el in oi.EnumerateArray())
            {
                var id = el.GetProperty("islandId").GetString() ?? string.Empty;
                var statusStr = el.GetProperty("newStatus").GetString() ?? "Organized";
                var status = Enum.TryParse<IslandStatus>(statusStr, ignoreCase: true, out var s) ? s : IslandStatus.Organized;
                organized.Add(new IslandOrganization(id, status));
            }
        }

        var decisions = new List<DecisionRecord>();
        if (root.TryGetProperty("decisions", out var dec))
        {
            foreach (var el in dec.EnumerateArray())
            {
                var id = el.GetProperty("id").GetString() ?? string.Empty;
                var desc = el.GetProperty("description").GetString() ?? string.Empty;
                var impact = el.GetProperty("impact").GetString() ?? string.Empty;
                decisions.Add(new DecisionRecord(id, desc, impact));
            }
        }

        return new OrganizeResult(rawOutput, gateSatisfied, organized, decisions);
    }
}

public record OrganizeResult(
    string Output,
    bool GateSatisfied,
    IReadOnlyList<IslandOrganization> OrganizedIslands,
    IReadOnlyList<DecisionRecord> Decisions) : StepResult(Output, GateSatisfied)
{
    public override void ApplyTo(AgentSession session)
    {
        session.Backlog.ApplyOrganization(OrganizedIslands);

        foreach (var dec in Decisions)
            session.RecordDecision(dec.Id, dec.Description, dec.Impact);
    }
}

public record IslandOrganization(string IslandId, IslandStatus NewStatus);

public record DecisionRecord(string Id, string Description, string Impact);
