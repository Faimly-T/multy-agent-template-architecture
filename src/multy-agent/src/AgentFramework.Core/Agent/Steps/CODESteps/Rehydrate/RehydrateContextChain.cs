using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.Rehydrate;

internal sealed class RehydrateContextChain
{
    private readonly IReadOnlyList<IRehydrateContextHandler> _handlers;

    public RehydrateContextChain(params IRehydrateContextHandler[] handlers)
    {
        _handlers = handlers;
    }

    public async Task<(string Output, IReadOnlyList<HandlerExchange> Journal)> RunAsync(
        IAgentRunContext? context,
        ISessionWriter writer,
        IChatClient chatClient,
        CancellationToken ct)
    {
        var journal = new List<HandlerExchange>();
        HandlerExchange? lastExchange = null;

        foreach (var handler in _handlers)
        {
            lastExchange = await handler.HandleAsync(lastExchange, context, writer, chatClient, ct);
            journal.Add(lastExchange);
        }

        var output = lastExchange?.Content.Output
            ?? throw new InvalidOperationException("Rehydrate chain completed without producing output.");

        return (output, journal);
    }
}
