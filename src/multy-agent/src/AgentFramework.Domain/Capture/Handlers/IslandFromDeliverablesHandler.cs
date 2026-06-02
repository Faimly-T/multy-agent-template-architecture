using System.Text.Json;
using System.Text.Json.Serialization;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.Capture.Handlers;

/// <summary>
/// Iterates over each deliverable in the session and asks the LLM whether the deliverable
/// reveals any user insight not yet captured as an island. A strict skill is injected to
/// keep the island list lean — the LLM defaults to returning the unchanged list.
///
/// Each LLM call receives:
///   • The session objective (from CaptureObjectiveContextCommand)
///   • The accumulated island list so far (grows with each iteration)
///   • The deliverable's path and status
///
/// When no deliverables exist the previous handler's output is passed through unchanged.
/// </summary>
internal sealed class IslandFromDeliverablesHandler : CommandHandlerBase
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private const string IslandSchema = """
        {
          "islands": [
            {
              "id": "ISL-XXX",
              "type": "UserType | Goal | PainPoint | BehavioralPattern | ContextOfUse | EmotionalState | AntiUser | Stakeholder | AccessibilitySignal",
              "description": "string",
              "source": "string",
              "relatesToIslandId": "ISL-XXX or null"
            }
          ]
        }
        """;

    private const string Instruction =
        "You are a strict UX research analyst reviewing a deliverable against the session objective. " +
        "Only add or modify an island when the deliverable clearly reveals a user insight not yet captured. " +
        "Default to returning the current list UNCHANGED. " +
        "Respond with valid JSON containing the COMPLETE island list (existing + any changes).";

    public IslandFromDeliverablesHandler(IStepPromptLayer? stepCtx = null)
        : base(stepCtx, Instruction) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext? agentContext,
        ISessionWriter    writer,
        IChatClient?      chatClient,
        CancellationToken ct)
    {
        var deliverables = agentContext?.Deliverables ?? [];

        // No deliverables — pass the island list from the previous handler through unchanged.
        if (deliverables.Count == 0)
        {
            var passThrough = context.GetOutput(nameof(IslandFromObjectiveHandler))
                ?? """{"islands":[]}""";
            return new HandlerExchange(
                GetType().Name,
                new HandlerContent(passThrough, passThrough),
                new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: false));
        }

        var objectiveContext = context.GetOutput(nameof(CaptureObjectiveContextCommand)) ?? string.Empty;
        var accumulated = ParseIslands(
            context.GetOutput(nameof(IslandFromObjectiveHandler)) ?? """{"islands":[]}""");

        // Iterate: each deliverable can add or refine islands; the context grows each pass.
        foreach (var deliverable in deliverables)
        {
            ct.ThrowIfCancellationRequested();

            var userContent = $"""
                {objectiveContext}

                Current island list (accumulated so far):
                {FormatIslands(accumulated)}

                Deliverable under review:
                  ID:     {deliverable.DeliverableId}
                  Path:   {deliverable.Path}
                  Status: {deliverable.Status}

                Apply strict criteria — add or modify an island ONLY if the deliverable reveals
                a user insight not already represented. Max 3 changes per pass.
                Return the COMPLETE island list (existing unchanged + any additions/modifications).
                """;

            var messages   = BuildPrompt(userContent, skillNames: ["capture-strict-islands"]);
            var resultJson = await chatClient!.SendHandlerAsync(
                messages, IslandSchema, root => root.GetRawText(), ct);

            var updated = ParseIslands(resultJson);
            accumulated = MergeIslands(accumulated, updated);
        }

        var finalJson = SerializeIslandList(accumulated);
        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(context.LastOutput() ?? string.Empty, finalJson),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }

    // --- Island helpers ---

    private static List<IslandDto> ParseIslands(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("islands", out var arr)) return [];
            return arr.EnumerateArray()
                .Select(el => new IslandDto(
                    el.TryGetProperty("id",                out var id)   ? id.GetString()   ?? string.Empty : string.Empty,
                    el.TryGetProperty("type",              out var type) ? type.GetString()  ?? "Goal"       : "Goal",
                    el.TryGetProperty("description",       out var desc) ? desc.GetString()  ?? string.Empty : string.Empty,
                    el.TryGetProperty("source",            out var src)  ? src.GetString()   ?? string.Empty : string.Empty,
                    el.TryGetProperty("relatesToIslandId", out var rel) && rel.ValueKind != JsonValueKind.Null
                        ? rel.GetString() : null))
                .Where(i => !string.IsNullOrWhiteSpace(i.Id))
                .ToList();
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse island JSON from prior handler: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Merges update list into current list. Same ID → replace. New ID → append.
    /// </summary>
    private static List<IslandDto> MergeIslands(List<IslandDto> current, List<IslandDto> updates)
    {
        var map = current.ToDictionary(i => i.Id);
        foreach (var u in updates)
            map[u.Id] = u;
        return [.. map.Values];
    }

    private static string FormatIslands(IReadOnlyList<IslandDto> islands) =>
        islands.Count == 0
            ? "  (none yet)"
            : string.Join('\n', islands.Select(i => $"  [{i.Id}] [{i.Type}] {i.Description}"));

    private static string SerializeIslandList(IReadOnlyList<IslandDto> islands)
    {
        var wrapper = new IslandListDto([.. islands]);
        return JsonSerializer.Serialize(wrapper, JsonOptions);
    }

    // --- DTOs ---

    private record IslandDto(
        string  Id,
        string  Type,
        string  Description,
        string  Source,
        string? RelatesToIslandId);

    private record IslandListDto(
        [property: JsonPropertyName("islands")]
        List<IslandDto> Islands);
}
