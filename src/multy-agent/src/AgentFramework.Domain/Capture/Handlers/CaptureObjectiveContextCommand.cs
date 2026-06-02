using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.Capture.Handlers;

/// <summary>
/// Entry point for the Capture chain.
/// Extracts the Kickoff-defined session objective and existing session state into a
/// structured context string that all subsequent capture handlers use as their base.
/// No LLM call — pure context assembly.
/// </summary>
internal sealed class CaptureObjectiveContextCommand : ICommandHandler
{
    public Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext? agentContext,
        ISessionWriter    writer,
        IChatClient?      _,
        CancellationToken ct)
    {
        var objective = agentContext?.Session?.CurrentCheckpoint?.SessionObjective
            ?? "No objective defined — capture broadly.";

        var existingIslands = agentContext?.Session?.Backlog.All ?? [];
        var deliverables    = agentContext?.Deliverables ?? [];

        var islandSection = existingIslands.Count > 0
            ? $"\n\nExisting islands ({existingIslands.Count} in backlog):\n" +
              string.Join('\n', existingIslands.Select(i =>
                  $"  [{i.Id}] [{i.Type}] {i.Description}"))
            : "\n\nNo existing islands — starting fresh.";

        var deliverableSection = deliverables.Count > 0
            ? "\n\nPrevious deliverables available for review:\n" +
              string.Join('\n', deliverables.Select(d =>
                  $"  [{d.DeliverableId}] {d.Path} (status: {d.Status})"))
            : string.Empty;

        var output = $"Session Objective: {objective}{islandSection}{deliverableSection}"; //TODO: we need to talk with the lllm and define the objective based on the goal from kickoff, and also use the inforamtion from the curren island section and deliverable section.

        return Task.FromResult(new HandlerExchange(
            GetType().Name,
            new HandlerContent(context.LastOutput() ?? "[start]", output),
            new HandlerMetadata(null, IsLlmCall: false)));
    }
}
