using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain;

internal sealed class KickoffContextChain
{
    private readonly IReadOnlyList<ICommandHandler> _handlers;

    public KickoffContextChain(params ICommandHandler[] handlers)
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

        foreach (var handler in _handlers)
        {
            var exchange = await handler.ExecuteAiCommandAsync(journal.AsReadOnly(), context, writer, chatClient, ct);
            journal.Add(exchange);
        }

        var output = journal.Count > 0
            ? journal[^1].Content.Output
            : throw new InvalidOperationException("Kickoff chain completed without producing output.");

        return (output, journal.AsReadOnly());
    }
}
