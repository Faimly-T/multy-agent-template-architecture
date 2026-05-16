
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


        Pipeline = UxStepBuilder.Create()
        // TODO: the steps should be dynamic and control by the pipeline framewrok CODE
        //First I need to build the pipeline strategy that I will use in this case CODE
        .WithSteps(pSteps) 
        // TODO: this is the list that I will load per aggregate and is the entities per each domain
        //Second I should build and attach the skills of the agent that I want to use in the execution of the steps, this is important because I can organize the agent capability based on the skills.
        .WithSkills(skills)
        //TODO: I need a mechanism to affect the logic for each step, example I want to have the skill to read the questions, a skill on how to build the islands, a skill to define how to express the final report, etc... 
        .Build();
    }
}