using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps;

internal sealed class DefaultStepCommand : CommandHandlerBase
{
    private readonly AgentStep _step;

    public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];
    public StepResult? LastResult { get; private set; }

    public DefaultStepCommand(IStepPromptLayer stepCtx, string instructions, AgentStep step)
        : base(stepCtx, instructions)
    {
        _step = step;
    }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext? contextAgent,
        ISessionWriter writer,
        IChatClient? chatClient,
        CancellationToken ct)
    {
        var userContext = _step.BuildContext(contextAgent);
        var schema = $"""
            ### Required Response Format
            Respond ONLY with a JSON object matching this schema:
            ```json
            {_step.JsonResponseSchema}
            ```
            Gate: {_step.Gate.Description}
            """;

        var fullUser = string.IsNullOrWhiteSpace(userContext)
            ? schema
            : $"{userContext}\n\n{schema}";

        var messages = BuildPrompt(fullUser);
        LastMessages = messages;

        var result = await chatClient!.SendAsync(messages, _step, ct);
        LastResult = result;

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(context.LastOutput() ?? "[start]", result.Output),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }
}
