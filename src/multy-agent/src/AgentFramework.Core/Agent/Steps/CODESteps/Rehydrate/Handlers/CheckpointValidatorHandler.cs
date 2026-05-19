using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.Rehydrate.Handlers;

internal class CheckpointValidatorCommand(Skill? skill = null) : ICommandHandler
{
    //TODO: I should define a base class with the sythesis instructions and the schema to mange the basic flow and control this command AI, actually I can also manage the skill and role injection in the base class and just pass them in the constructor of the handlers that need them, this will also help with prompt consistency across handlers.
    private const string SynthesisInstruction =
        "You are an assistant helping to rehydrate the context of an agent's previous session, to define the Goal of the next iteration.  Review the following list of previous checkpoints and generate a summary context of the previous sessions goals as valid information to be used in the definition of the next goal based on the user request. ";

    private const string Schema = """
        {
          "sessionContext": "Summary context of previous sessions goals to be used in the definition of the next goal based on the user request"
        }
        """;

    public async Task<HandlerExchange> ExecuteAiCommandAsync(
        HandlerExchange? previousExchange,
        IAgentRunContext? contextAgent, 
        ISessionWriter writer,
        IChatClient chatClient, 
        CancellationToken ct)
    {
        var promptCommandAiParts = new List<string>();

        if (contextAgent?.Role is not null)
            promptCommandAiParts.Add(contextAgent.Role.BuildRolePrompt());

        if (skill is not null)
            promptCommandAiParts.Add($"### Skill: {skill.Name}\n{skill.Instructions}");

        promptCommandAiParts.Add(SynthesisInstruction);

        var line = new List<string>();

        foreach (var checkpoint in 
            contextAgent?.Session?.Checkpoints.OrderByDescending(c => c.SessionIteration) ?? Enumerable.Empty<Checkpoint>())
        {
                line.Add($"Checkpoint #{checkpoint.SessionIteration} " +
                            $"({checkpoint.CreatedAt:yyyy-MM-dd}): " +
                            $"Focused on {checkpoint.SessionObjective}. " +
                            $"Accomplished: {string.Join(", ", checkpoint.Accomplishments?.Take(3) ?? Enumerable.Empty<string>())}");
        }

        var userContent = $"""            
            Previous checkpoints: 
            {string.Join("\n", line)}
            """;

        IReadOnlyList<ChatMessage> messages = [
            new ChatMessage(MessageRole.System, string.Join("\n\n", promptCommandAiParts)),
            new ChatMessage(MessageRole.User, userContent)
        ];

        var json = await chatClient.SendHandlerAsync(messages, Schema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(previousExchange?.Content.Output ?? "[start]", json),
            new HandlerMetadata(previousExchange?.Sender, IsLlmCall: true));
    }

    private HandlerExchange Exchange(HandlerExchange? previous, string output) =>
        new(GetType().Name,
            new HandlerContent(previous?.Content.Output ?? "[start]", output),
            new HandlerMetadata(previous?.Sender, IsLlmCall: false));
}
