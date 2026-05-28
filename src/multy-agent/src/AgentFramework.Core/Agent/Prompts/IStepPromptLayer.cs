using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Prompts;

public interface IStepPromptLayer
{
    string?              RolePrompt { get; }
    IReadOnlyList<Skill> Skills     { get; }
    IStepPromptLayer     WithSkills(IReadOnlyList<Skill> skills);
}
