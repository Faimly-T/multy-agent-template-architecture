
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

public class UxPersona : AgentAggregate<string>
{
    // Steps are built externally (via UxStepBuilder) and injected here.
    // The constructor wraps them in a StepPipeline that manages cursor and traversal.
    public UxPersona(Role pRole, AgentStep[] pSteps) : base("ux-persona-architect", pRole)
    {
        if (pRole is null)
            throw new InvalidOperationException("Role is required. Call WithRole() before Build().");

        if (pSteps.Length == 0)
            throw new InvalidOperationException("At least one step is required. Call WithSteps() before Build().");


        Pipeline = new StepPipeline(pSteps);
    }
}