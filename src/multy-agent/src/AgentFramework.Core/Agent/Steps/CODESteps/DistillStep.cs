using System.Text.Json;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps;

public class DistillStep : AgentStep
{
    public DistillStep(int stepNumber, string name, string instructions, Gate gate)
        : base(stepNumber, name, "expert-distill", instructions, gate) { }

    public override string JsonResponseSchema => """
        {
          "distilledIslands": [
            {
              "islandId": "ISL-XXX",
              "newStatus": "Distilled | Discarded"
            }
          ],
          "deliverables": [
            {
              "deliverableId": "DEL-XXX",
              "path": "outputs/folder/filename.md",
              "status": "Draft | Partial | Complete"
            }
          ],
          "gateSatisfied": true
        }
        """;

    public override string BuildContext(AgentSession? session)
    {
        if (session is null) return "No session context";

        var organized = session.Islands
            .Where(i => i.Status == IslandStatus.Organized)
            .Select(i => $"- {i.Id} [{i.Type}] {i.Description}");

        var decisions = session.Decisions
            .Select(d => $"- {d.Id}: {d.Description} (Impact: {d.Impact})");

        return $"""
            Organized Islands:
            {string.Join("\n", organized)}

            Decisions so far:
            {(decisions.Any() ? string.Join("\n", decisions) : "None")}
            """;
    }

    public override StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied)
    {
        var distilled = new List<IslandDistillation>();
        if (root.TryGetProperty("distilledIslands", out var di))
        {
            foreach (var el in di.EnumerateArray())
            {
                var id = el.GetProperty("islandId").GetString() ?? string.Empty;
                var statusStr = el.GetProperty("newStatus").GetString() ?? "Distilled";
                var status = Enum.TryParse<IslandStatus>(statusStr, ignoreCase: true, out var s) ? s : IslandStatus.Distilled;
                distilled.Add(new IslandDistillation(id, status));
            }
        }

        var deliverables = new List<DeliverableRecord>();
        if (root.TryGetProperty("deliverables", out var del))
        {
            foreach (var el in del.EnumerateArray())
            {
                var delivId = el.GetProperty("deliverableId").GetString() ?? string.Empty;
                var path = el.GetProperty("path").GetString() ?? string.Empty;
                var statusStr = el.GetProperty("status").GetString() ?? "Draft";
                var status = Enum.TryParse<DeliverableStatus>(statusStr, ignoreCase: true, out var s) ? s : DeliverableStatus.Draft;
                deliverables.Add(new DeliverableRecord(delivId, path, status));
            }
        }

        return new DistillResult(rawOutput, gateSatisfied, distilled, deliverables);
    }
}

public record DistillResult(
    string Output,
    bool GateSatisfied,
    IReadOnlyList<IslandDistillation> DistilledIslands,
    IReadOnlyList<DeliverableRecord> Deliverables) : StepResult(Output, GateSatisfied)
{
    public override void ApplyTo(AgentSession session)
    {
        foreach (var dist in DistilledIslands)
        {
            var island = session.FindIsland(dist.IslandId);
            if (island is null) continue;

            switch (dist.NewStatus)
            {
                case IslandStatus.Distilled: island.Distill(); break;
                case IslandStatus.Discarded: island.Discard(); break;
            }
        }

        foreach (var del in Deliverables)
            session.RecordDeliverable(del.DeliverableId, del.Path, del.Status);
    }
}

public record IslandDistillation(string IslandId, IslandStatus NewStatus);

public record DeliverableRecord(string DeliverableId, string Path, DeliverableStatus Status);
