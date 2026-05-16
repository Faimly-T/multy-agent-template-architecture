using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Ports;

public interface IDeliverableWriter
{
    Task WriteAsync(IAgentRunContext context, IReadOnlyList<StepResult> results, CancellationToken ct = default);
}
