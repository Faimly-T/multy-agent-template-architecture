using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain;

internal interface ICommandHandler
{
    string Name => GetType().Name;

    Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext? contextAgent,
        ISessionWriter writer,
        IChatClient? chatClient,
        CancellationToken ct);
}
