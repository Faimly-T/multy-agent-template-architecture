using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.Rehydrate;

internal interface ICommandHandler
{
    string Name => GetType().Name;

    // Input:  the HandlerExchange produced by the previous handler (null for the first handler)
    // Output: a new HandlerExchange this handler constructs — becomes the next handler's input
    Task<HandlerExchange> ExecuteAiCommandAsync(
        HandlerExchange? previousExchange,  //TODO: Remove this from the command
        IAgentRunContext? contextAgent,
        ISessionWriter writer,
        IChatClient? chatClient,
        CancellationToken ct);
}
