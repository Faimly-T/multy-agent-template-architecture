using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.Rehydrate;

internal interface IRehydrateContextHandler
{
    string Name => GetType().Name;

    // Input:  the HandlerExchange produced by the previous handler (null for the first handler)
    // Output: a new HandlerExchange this handler constructs — becomes the next handler's input
    Task<HandlerExchange> HandleAsync(
        HandlerExchange? previousExchange,
        IAgentRunContext? context,
        ISessionWriter writer,
        IChatClient chatClient,
        CancellationToken ct);
}
