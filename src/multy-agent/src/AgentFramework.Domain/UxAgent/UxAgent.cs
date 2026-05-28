using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

public class UxPersona : AgentAggregate<string>
{
    public UxPersona(
        Role pRole,
        AgentStep[] pSteps,
        string projectId,
        SessionMarkFilePaths markFilePaths)
        : base("ux-persona-architect", pRole, projectId, markFilePaths)
    {
        if (pRole is null)
            throw new InvalidOperationException("Role is required.");

        if (pSteps.Length == 0)
            throw new InvalidOperationException("At least one step is required.");

        Pipeline = UxStepBuilder.Create().WithSteps(pSteps).Build();
    }
}
