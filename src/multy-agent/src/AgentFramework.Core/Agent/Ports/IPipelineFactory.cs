using AgentFramework.Core.Agent.Prompts;

namespace AgentFramework.Core.Agent.Ports;

public interface IPipelineFactory
{
    Task<Steps.StepPipeline> CreatePipelineAsync(
        IStepPromptLayer agentContext,
        CancellationToken ct = default);
}
