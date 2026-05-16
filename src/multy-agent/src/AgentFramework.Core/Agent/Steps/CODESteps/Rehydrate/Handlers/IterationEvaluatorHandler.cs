using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.Rehydrate.Handlers;

internal sealed class IterationEvaluatorHandler : IRehydrateContextHandler
{
    public Task<HandlerExchange> HandleAsync(
        HandlerExchange? previousExchange,
        IAgentRunContext? context, ISessionWriter writer,
        IChatClient _, CancellationToken ct)
    {
        var session = context?.Session;
        var history = session?.History;

        string output;
        if (history is null || session is null)
        {
            output = "Session #1: initial session, starting without prior context.";
        }
        else
        {
            var iteration = session.Checkpoints.Count;
            var daysSince = (DateTime.UtcNow - history.LastCheckpointDate).Days;

            var lines = new List<string>
            {
                $"Session #{iteration}: continuing from session #{history.LastSessionIteration}."
            };

            if (history.MomentumHeading is not null)
                lines.Add($"Recommended next focus: {history.MomentumHeading}.");
            if (history.ConfidenceLevel is not null)
                lines.Add($"Prior confidence level: {history.ConfidenceLevel}.");
            if (daysSince > 3)
                lines.Add($"Warning: {daysSince} days since last checkpoint — priorities may have shifted.");

            output = string.Join('\n', lines);
        }

        return Task.FromResult(Exchange(previousExchange, output));
    }

    private HandlerExchange Exchange(HandlerExchange? previous, string output) =>
        new(GetType().Name,
            new HandlerContent(previous?.Content.Output ?? "[start]", output),
            new HandlerMetadata(previous?.Sender, IsLlmCall: false));
}
