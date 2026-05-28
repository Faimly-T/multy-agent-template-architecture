using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain.Handlers;

//TODO: Evaluate if I should remove this class because this overlaps with the first CheckpointValidatorCommand
internal sealed class IterationEvaluatorHandler : ICommandHandler
{
    public Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext? agentContext, ISessionWriter writer,
        IChatClient? _, CancellationToken ct)
    {
        var session = agentContext?.Session;
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

        return Task.FromResult(new HandlerExchange(
            GetType().Name,
            new HandlerContent(context.LastOutput() ?? "[start]", output),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: false)));
    }
}
