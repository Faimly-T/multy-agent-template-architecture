using System.Text.Json;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Infrastructure.Anthropic;

internal static class StepResultParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static StepResult Parse(string json, string rawOutput, string skillName)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var gateSatisfied = root.TryGetProperty("gateSatisfied", out var g) && g.GetBoolean();

        return skillName switch
        {
            "rehydrate-context" => ParseRehydrate(root, rawOutput, gateSatisfied),
            "autonomous-capture" => ParseCapture(root, rawOutput, gateSatisfied),
            "strategic-organize" => ParseOrganize(root, rawOutput, gateSatisfied),
            "expert-distill" => ParseDistill(root, rawOutput, gateSatisfied),
            "express-relay" => ParseRelay(root, rawOutput, gateSatisfied),
            _ => new StepResult(rawOutput, gateSatisfied)
        };
    }

    private static RehydrateResult ParseRehydrate(JsonElement root, string output, bool gate)
    {
        var objective = root.GetProperty("sessionObjective").GetString() ?? string.Empty;
        return new RehydrateResult(output, gate, objective);
    }

    private static CaptureResult ParseCapture(JsonElement root, string output, bool gate)
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
        return new CaptureResult(output, gate, islands);
    }

    private static OrganizeResult ParseOrganize(JsonElement root, string output, bool gate)
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

        return new OrganizeResult(output, gate, organized, decisions);
    }

    private static DistillResult ParseDistill(JsonElement root, string output, bool gate)
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

        return new DistillResult(output, gate, distilled, deliverables);
    }

    private static RelayResult ParseRelay(JsonElement root, string output, bool gate)
    {
        var input = root.TryGetProperty("inputTokens", out var it) ? it.GetInt32() : 0;
        var outputTokens = root.TryGetProperty("outputTokens", out var ot) ? ot.GetInt32() : 0;
        return new RelayResult(output, gate, input, outputTokens);
    }
}
