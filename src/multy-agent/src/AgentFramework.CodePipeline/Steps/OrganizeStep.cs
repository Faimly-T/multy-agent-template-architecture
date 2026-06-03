using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.CodePipeline;

public class OrganizeStep : AgentStep
{
    public OrganizeStep(
        IStepPromptLayer       stepContext,
        int                    stepNumber,
        string                 instructions,
        Gate                   gate,
        IStepChain?            chain             = null,
        Func<string, string?>? instructionLookup = null)
        : base(stepContext, stepNumber, instructions, gate, chain, instructionLookup) { }

    /// <summary>Gate: at least one group must exist that is not explicitly Blocked.</summary>
    protected override bool EvaluateGate(JsonElement root, IReadOnlyList<HandlerExchange> journal)
    {
        if (!root.TryGetProperty("groups", out var grpArr) || grpArr.GetArrayLength() == 0)
            return false;
        foreach (var g in grpArr.EnumerateArray())
            if (!string.Equals(
                    g.TryGetProperty("readiness", out var r) ? r.GetString() : "Ready",
                    "Blocked", StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    public override string JsonResponseSchema => """
        {
          "groups": [
            {
              "id": "GRP-001",
              "name": "string",
              "islandIds": ["ISL-XXX"],
              "whyTogether": "string",
              "readiness": "Ready | OneGap | NeedsCapture | Blocked",
              "readinessNotes": "string or null"
            }
          ],
          "organizedIslands": [
            {
              "islandId": "ISL-XXX",
              "newStatus": "Organized | Discarded",
              "groupId": "GRP-001 or null"
            }
          ],
          "gateSatisfied": true
        }
        """;

    public override string BuildContext(IAgentRunContext? context)
    {
        var session = context?.Session;
        if (session is null || session.Backlog.Count == 0)
            return "Current Islands: None captured yet";

        var lines = session.Backlog.All.Select(i =>
            $"- {i.Id} [{i.Type}] {i.Description} (Status: {i.Status})" +
            (i.RelatesToIslandId is not null ? $" → relates to {i.RelatesToIslandId}" : ""));
        return $"Current Islands:\n{string.Join("\n", lines)}";
    }

    public override StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied)
    {
        var groups = new List<IslandGroup>();
        if (root.TryGetProperty("groups", out var grpArr))
        {
            foreach (var el in grpArr.EnumerateArray())
            {
                var id    = el.TryGetProperty("id",           out var idEl)    ? idEl.GetString()    ?? string.Empty : string.Empty;
                var name  = el.TryGetProperty("name",         out var nameEl)  ? nameEl.GetString()  ?? string.Empty : string.Empty;
                var why   = el.TryGetProperty("whyTogether",  out var whyEl)   ? whyEl.GetString()   ?? string.Empty : string.Empty;
                var ready = el.TryGetProperty("readiness",    out var readyEl) ? readyEl.GetString() ?? "Ready"      : "Ready";
                var notes = el.TryGetProperty("readinessNotes", out var notesEl) && notesEl.ValueKind != JsonValueKind.Null
                    ? notesEl.GetString() : null;

                var islandIds = new List<string>();
                if (el.TryGetProperty("islandIds", out var ids))
                    foreach (var idEl2 in ids.EnumerateArray())
                        if (idEl2.GetString() is { Length: > 0 } iid) islandIds.Add(iid);

                if (!string.IsNullOrWhiteSpace(id))
                    groups.Add(new IslandGroup(id, name, islandIds.AsReadOnly(), why, ready, notes));
            }
        }

        var organized = new List<IslandOrganization>();
        if (root.TryGetProperty("organizedIslands", out var oi))
        {
            foreach (var el in oi.EnumerateArray())
            {
                var id        = el.TryGetProperty("islandId",  out var idEl)  ? idEl.GetString()     ?? string.Empty : string.Empty;
                var statusStr = el.TryGetProperty("newStatus", out var stEl)  ? stEl.GetString()     ?? "Organized"  : "Organized";
                var groupId   = el.TryGetProperty("groupId",   out var grpEl) && grpEl.ValueKind != JsonValueKind.Null
                    ? grpEl.GetString() : null;
                var status    = Enum.TryParse<IslandStatus>(statusStr, ignoreCase: true, out var s) ? s : IslandStatus.Organized;
                if (!string.IsNullOrWhiteSpace(id))
                    organized.Add(new IslandOrganization(id, status, groupId));
            }
        }

        return new OrganizeResult(rawOutput, gateSatisfied, organized, [], groups);
    }
}

public record OrganizeResult(
    string Output,
    bool GateSatisfied,
    IReadOnlyList<IslandOrganization> OrganizedIslands,
    IReadOnlyList<DecisionRecord> Decisions,
    IReadOnlyList<IslandGroup>? Groups = null) : StepResult(Output, GateSatisfied)
{
    public override void ApplyTo(ISessionWriter writer)
    {
        writer.ApplyOrganization(OrganizedIslands, Decisions, Groups ?? []);
    }
}
