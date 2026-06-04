using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.Capture.Handlers;

/// <summary>
/// Applies the Six Thinking Hats method to the session objective to surface up to 10
/// high-quality islands. Each hat perspective reveals a different dimension of the user landscape.
/// Output is a JSON island list that feeds the rest of the capture chain.
/// </summary>
internal sealed class IslandFromObjectiveHandler : CommandHandlerBase
{
    // Schema lives in BrainAggregate — handlers reference it so the LLM format stays in sync with Brain's parser.
    private static string IslandSchema => BrainAggregate.IslandSchema;

    private const string Instruction =
        "You are a UX research analyst applying the Six Thinking Hats method to identify user insights. " +
        "Review the session objective and define between 3 and 10 high-quality islands that represent distinct " +
        "user types, goals, pain points, and behavioral patterns. " +
        "You MUST generate at least 3 islands — one per hat perspective at minimum. " +
        "Respond with valid JSON only.";

    public IslandFromObjectiveHandler(IStepPromptLayer? stepCtx = null)
        : base(stepCtx, Instruction) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext? agentContext,
        ISessionWriter    writer,
        IChatClient?      chatClient,
        CancellationToken ct)
    {
        var objectiveContext = context.LastOutput() ?? string.Empty;

        var userContent = $$"""
            {{objectiveContext}}

            Apply the Six Thinking Hats to identify up to 10 islands that will guide this capture session:

            White Hat  — Facts & data: What do we know about the users? What factual behaviours exist?
            Red Hat    — Emotions: How do users feel? What emotional needs are unmet?
            Black Hat  — Caution: What pain points, barriers, and friction do users face?
            Yellow Hat — Optimism: What goals, desired outcomes, and value do users seek?
            Green Hat  — Creativity: What unconventional user types or surprising behaviours might emerge?
            Blue Hat   — Structure: What overarching journey patterns or systemic user needs matter?

            Rules:
            - One island per distinct insight; use ISL-001 through ISL-010.
            - Set source to "six-hats:{hat-color}" (e.g. "six-hats:black").
            - Minimum 3 islands, maximum 10 — cover at least three hat perspectives.
            - Do not duplicate any existing islands listed in the context.

            Respond using EXACTLY this JSON structure — use these field names verbatim:
            {{IslandSchema}}
            """;

        var messages = BuildPrompt(userContent, skillNames: ["six-thinking-hats"]);
        var json = await chatClient!.SendHandlerAsync(messages, IslandSchema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(objectiveContext, json),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }
}
