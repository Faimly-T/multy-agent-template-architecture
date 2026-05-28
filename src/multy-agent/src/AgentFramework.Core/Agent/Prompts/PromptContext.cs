using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Prompts;

public sealed record PromptContext : IAgentPromptLayer, IStepPromptLayer
{
    public string?              RolePrompt { get; init; }
    public IReadOnlyList<Skill> Skills     { get; init; } = [];

    public static PromptContext Empty => new();

    IStepPromptLayer IAgentPromptLayer.WithRole(Role role)
        => this with { RolePrompt = role?.BuildRolePrompt };

    IStepPromptLayer IStepPromptLayer.WithSkills(IReadOnlyList<Skill> skills)
        => this with { Skills = skills };
}
