
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

public class UxPersona : AgentAggregate<string>
{
    // Steps and skills are built externally and injected here.
    // The constructor wraps them in a StepPipeline that manages cursor, traversal, and skill attachment.
    public UxPersona(Role pRole, AgentStep[] pSteps, IEnumerable<Skill>? skills = null) : base("ux-persona-architect", pRole)
    {
        if (pRole is null)
            throw new InvalidOperationException("Role is required. Call WithRole() before Build().");

        if (pSteps.Length == 0)
            throw new InvalidOperationException("At least one step is required. Call WithSteps() before Build().");


        Pipeline = UxStepBuilder.Create().WithSteps(pSteps).WithSkills(skills).Build();
    }
}