using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Ports;

public interface IPipelineFactory
{
    Task<StepPipeline> CreatePipelineAsync(Role role, ISkillProvider skillProvider, CancellationToken ct = default);
}