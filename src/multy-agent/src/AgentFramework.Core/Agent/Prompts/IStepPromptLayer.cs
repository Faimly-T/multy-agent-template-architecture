using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Prompts;

public interface IStepPromptLayer
{
    RoleDefinition?      Role       { get; }
    string?              RolePrompt { get; }
    IReadOnlyList<Skill> Skills     { get; }
    IStepPromptLayer     WithSkills(IReadOnlyList<Skill> skills);
}