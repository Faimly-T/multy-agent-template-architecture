namespace AgentFramework.Core.Agent.Steps;

public record StepSkillConfig(
    string Instructions,
    Gate Gate,
    IReadOnlyList<string> SkillNames);
