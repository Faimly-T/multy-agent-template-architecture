using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.Organize.Handlers;

/// <summary>
/// Takes all captured islands and groups them semantically, identifying the shared concern
/// for each group (the WHY). Each group gets a sharp name that captures what the islands are saying.
/// </summary>
internal sealed class IslandGroupingHandler : CommandHandlerBase
{
    private const string GroupingSchema = """
        {
          "groups": [
            {
              "id": "GRP-001",
              "name": "string — sharp name capturing what the islands are saying",
              "islandIds": ["ISL-XXX"],
              "whyTogether": "string — one sentence: what these islands are all expressing"
            }
          ],
          "ungroupedIslandIds": ["ISL-XXX"]
        }
        """;

    private const string Instruction =
        "You are a strategic analyst applying semantic grouping to a list of research islands. " +
        "Group islands that share the same underlying concern, person, goal, or root cause. " +
        "Each group name should capture what the islands are SAYING, not just their topic. " +
        "Minimum 2 islands per group. Islands with no match go into ungroupedIslandIds. " +
        "Respond with valid JSON only.";

    public IslandGroupingHandler(IStepPromptLayer? stepCtx = null)
        : base(stepCtx, Instruction) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext? agentContext,
        ISessionWriter    writer,
        IChatClient?      chatClient,
        CancellationToken ct)
    {
        var session   = agentContext?.Session;
        var islands   = session?.Backlog.All ?? [];
        var objective = session?.CurrentCheckpoint?.SessionObjective ?? "No objective defined";

        var islandList = string.Join("\n", islands.Select(i =>
            $"- {i.Id} [{i.Type}]: {i.Description}" +
            (i.RelatesToIslandId is not null ? $" (relates to {i.RelatesToIslandId})" : "")));

        var userContent = $"""
            Session Objective: {objective}

            Islands to group:
            {islandList}

            Instructions:
            - Identify semantic groups: islands expressing the same underlying concern.
            - Group name: short, sharp — captures what the islands ARE saying (e.g. "Max — Elite Ready", not "Max's Islands").
            - whyTogether: one sentence — the shared root cause or concern.
            - Minimum 2 islands per group. Islands that don't fit any group → ungroupedIslandIds.
            - Use GRP-001, GRP-002, ... for group IDs.

            Respond using EXACTLY this JSON structure — use these field names verbatim:
            {GroupingSchema}
            """;

        var messages = BuildPrompt(userContent, skillNames: ["strategic-organize"]);
        var json     = await chatClient!.SendHandlerAsync(messages, GroupingSchema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(islandList, json),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }
}
