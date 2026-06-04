using System.Text;
using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.Organize.Handlers;

/// <summary>
/// Final no-LLM handler in the Organize chain.
/// Reads the groups with readiness from the previous handler and produces the complete
/// OrganizeStep JSON: groups + organizedIslands (each island assigned to its group) + gateSatisfied.
/// </summary>
internal sealed class OrganizeSynthesisHandler : ICommandHandler
{
    public Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext?  agentContext,
        ISessionWriter     writer,
        IChatClient?       _,
        CancellationToken  ct)
    {
        var readinessJson = context.LastOutput() ?? """{"groups":[]}""";

        var finalJson = BuildOrganizeJson(readinessJson, agentContext);

        return Task.FromResult(new HandlerExchange(
            GetType().Name,
            new HandlerContent(readinessJson, finalJson),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: false)));
    }

    private static string BuildOrganizeJson(string readinessJson, IAgentRunContext? agentContext)
    {
        using var doc  = JsonDocument.Parse(readinessJson);
        var groups     = doc.RootElement.TryGetProperty("groups", out var grpsEl) ? grpsEl : default;

        // Collect all island IDs present in any group
        var groupedIds = new Dictionary<string, string>(); // islandId → groupId
        var groupList  = new List<JsonElement>();

        if (groups.ValueKind == JsonValueKind.Array)
        {
            foreach (var grp in groups.EnumerateArray())
            {
                groupList.Add(grp);
                var gid = grp.TryGetProperty("id", out var gEl) ? gEl.GetString() ?? string.Empty : string.Empty;
                if (grp.TryGetProperty("islandIds", out var idsArr))
                    foreach (var idEl in idsArr.EnumerateArray())
                        if (idEl.GetString() is { Length: > 0 } iid)
                            groupedIds[iid] = gid;
            }
        }

        // All session islands not yet assigned to a group → standalone (groupId: null)
        var allIslands = agentContext?.Brain?.Backlog.All ?? [];
        var organized  = new List<(string IslandId, string? GroupId)>();
        foreach (var island in allIslands)
            if (island.Status == Core.Agent.Session.IslandStatus.Captured)
                organized.Add((island.Id, groupedIds.TryGetValue(island.Id, out var gid) ? gid : null));

        // Gate: any group that is not explicitly Blocked can proceed to Distill.
        // Ready/OneGap/NeedsCapture all allow distillation; Distill raises questions for gaps.
        var readyGroupExists = groupList.Any(g =>
        {
            if (!g.TryGetProperty("readiness", out var rEl)) return true;
            return !string.Equals(rEl.GetString(), "Blocked", StringComparison.OrdinalIgnoreCase);
        });

        using var ms     = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms);

        writer.WriteStartObject();

        // groups — pass through from readiness handler
        writer.WritePropertyName("groups");
        if (groups.ValueKind == JsonValueKind.Array)
            groups.WriteTo(writer);
        else
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
        }

        // organizedIslands
        writer.WritePropertyName("organizedIslands");
        writer.WriteStartArray();
        foreach (var (islandId, groupId) in organized)
        {
            var newStatus = groupId is not null ? "Organized" : "Discarded";
            writer.WriteStartObject();
            writer.WriteString("islandId",  islandId);
            writer.WriteString("newStatus", newStatus);
            if (groupId is not null) writer.WriteString("groupId", groupId);
            else writer.WriteNull("groupId");
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteBoolean("gateSatisfied", readyGroupExists && organized.Count > 0);

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
