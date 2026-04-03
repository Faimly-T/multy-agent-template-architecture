namespace AgentFramework.Core.Agent.Steps;

public record AgentStep(
    int StepNumber,
    string Name,
    string SkillName,
    string Instructions,
    Gate Gate,
    string JsonResponseSchema)
{
    public Skill? Skill { get; private set; }

    public AgentStep WithSkill(Skill skill) =>
        this with { Skill = skill };
}
