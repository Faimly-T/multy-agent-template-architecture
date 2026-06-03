using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Ports;

/// <summary>
/// Port for writing the agent's output artefacts after a pipeline run completes.
/// Called by <see cref="AgentAggregate{TId}.RunAsync"/> after all steps finish (including ClosedStep).
/// </summary>
public interface IDeliverableWriter
{
    Task WriteAsync(IAgentRunContext context, IReadOnlyList<StepResult> results, CancellationToken ct = default);
}

/// <summary>
/// Null Object implementation — discards all deliverables silently.
/// Use in tests and scenarios where output artefacts are not needed.
/// </summary>
public sealed class NullDeliverableWriter : IDeliverableWriter
{
    public static readonly NullDeliverableWriter Instance = new();
    private NullDeliverableWriter() { }
    public Task WriteAsync(IAgentRunContext context, IReadOnlyList<StepResult> results, CancellationToken ct = default)
        => Task.CompletedTask;
}
