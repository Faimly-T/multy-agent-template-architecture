using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

public class UxStepBuilder
{
    private readonly List<AgentStep> _steps = [];
    private readonly List<Skill> _skills = [];

    public static UxStepBuilder Create() => new();

    public UxStepBuilder WithStep(AgentStep step)
    {
        _steps.Add(step);
        return this;
    }

    public UxStepBuilder WithSteps(IEnumerable<AgentStep> steps)
    {
        _steps.AddRange(steps);
        return this;
    }

    public UxStepBuilder WithSkills(IEnumerable<Skill> skills)
    {
        _skills.AddRange(skills);
        return this;
    }

    public AgentStep[] Build()
    {
        foreach (var skill in _skills)
        {
            var step = _steps.FirstOrDefault(s => s.SkillName == skill.Name);
            step?.AttachSkill(skill);
        }

        return [.. _steps];
    }
}
