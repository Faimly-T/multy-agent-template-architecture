using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

public class UxPersona : AgentAggregate<string>
{
    private UxPersona(
        string               agentId,
        IStepPromptLayer     agentContext,
        StepPipeline         pipeline,
        string               projectId,
        SessionMarkFilePaths markFilePaths)
        : base(agentId, agentContext, projectId, markFilePaths)
    {
        Pipeline = pipeline;
    }

    public static async Task<UxPersona> BuildAsync(
        UxPersonaConfig   config,
        PipelineCode      pipelineCode,
        CancellationToken ct = default)
    {
        var agentContext = ((IAgentPromptLayer)PromptContext.Empty).WithRole(config.Role);
        var pipeline     = await pipelineCode.BuildAsync(agentContext, config.Pipeline, ct);
        return new UxPersona(config.AgentId, agentContext, pipeline, config.ProjectId, config.MarkFilePaths);
    }
}
