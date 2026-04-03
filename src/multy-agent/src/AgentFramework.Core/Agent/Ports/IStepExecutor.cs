using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Ports;

public interface IStepExecutor
{
    Task<StepResult> ExecuteAsync(AgentStep step, Role role, CancellationToken ct = default);
}
