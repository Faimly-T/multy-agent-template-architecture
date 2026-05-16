using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

public class UxStepBuilder
{
    private readonly List<AgentStep> _steps = [];
    private IEnumerable<Skill>? _skills;

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

    public UxStepBuilder WithSkills(IEnumerable<Skill>? skills)
    {
        _skills = skills;
        return this;
    }

    public StepPipeline Build()
    {
        if (_skills is not null)
            AttachSkills();

        ValidateSkills();

        return new StepPipeline(_steps);
    }

    private void AttachSkills()
    {
        var skillMap = _skills!.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var step in _steps)
        {
            if (!string.IsNullOrEmpty(step.SkillName) && skillMap.TryGetValue(step.SkillName, out var skill))
                step.AttachSkill(skill); 
        }
    }

    private void ValidateSkills()
    {
        var missing = _steps
            .Where(s => !string.IsNullOrEmpty(s.SkillName) && s.Skill is null)
            .ToList();

        if (missing.Count > 0)
        {
            var details = string.Join(", ", missing.Select(s => $"Step {s.StepNumber} '{s.Name}' requires skill '{s.SkillName}'"));
            throw new InvalidOperationException($"Steps are missing required skills: {details}");
        }
    }
}
