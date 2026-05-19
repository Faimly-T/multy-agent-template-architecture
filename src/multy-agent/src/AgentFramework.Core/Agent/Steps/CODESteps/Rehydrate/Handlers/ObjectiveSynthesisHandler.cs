using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.Rehydrate.Handlers;

internal sealed class ObjectiveSynthesisHandler(Skill? skill = null) : ICommandHandler
{
    private const string SynthesisInstruction =
        "Synthesize a precise session objective from the context below. " +
        "Respond with valid JSON only.";

    private const string Schema = """
        {
          "sessionObjective": "verb + deliverable + success condition + stakes clause",
          "narrativeBridge": "2-3 sentences connecting prior session to this one",
          "isInitialSession": false,
          "stalenessWarning": "string or null",
          "blockers": [{ "questionId": "Q-001", "text": "...", "severity": "hard|soft" }],
          "gateSatisfied": true
        }
        """;

    public async Task<HandlerExchange> ExecuteAiCommandAsync(
        HandlerExchange? previousExchange,
        IAgentRunContext? contextAgent, 
        ISessionWriter writer,
        IChatClient chatClient, 
        CancellationToken ct)
    {
        var messages = BuildMessages(
            previousExchange?.Content.Output ?? string.Empty, 
            contextAgent);

        var json = await chatClient.SendHandlerAsync(messages, Schema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(previousExchange?.Content.Output ?? "[start]", json),
            new HandlerMetadata(previousExchange?.Sender, IsLlmCall: true));
    }

    private IReadOnlyList<ChatMessage> BuildMessages(
        string triageContext, 
        IAgentRunContext? context)
    {
        var systemParts = new List<string>();

        if (context?.Role is not null)
            systemParts.Add(context.Role.BuildRolePrompt());

        if (skill is not null)
            systemParts.Add($"### Skill: {skill.Name}\n{skill.Instructions}");

        systemParts.Add(SynthesisInstruction);

        var userInput = context?.Session?.CurrentCheckpoint?.SessionObjective;
        var userInputLine = string.IsNullOrWhiteSpace(userInput)
            ? string.Empty
            : $"\nUser stated intent: {userInput}\n";

        var userContent = $"""
            {triageContext}{userInputLine}

            Synthesize:
              1. sessionObjective — verb + deliverable + success condition + stakes clause
              2. narrativeBridge — 2-3 sentences connecting prior session end to this session start
              3. isInitialSession — true only if no prior session exists
              4. stalenessWarning — include if checkpoint is stale, otherwise null
              5. blockers — list of still-open blockers from prior context, with questionId, text, and severity
              6. gateSatisfied — true if objective is well-formed, false if missing required elements
            """;

        return
        [
            new ChatMessage(MessageRole.System, string.Join("\n\n", systemParts)),
            new ChatMessage(MessageRole.User, userContent)
        ];
    }
}
