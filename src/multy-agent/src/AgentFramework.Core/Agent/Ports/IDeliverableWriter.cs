using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Ports;

public interface IDeliverableWriter
{
    Task WriteAsync(AgentSession session, IReadOnlyList<StepResult> results, CancellationToken ct = default);
}