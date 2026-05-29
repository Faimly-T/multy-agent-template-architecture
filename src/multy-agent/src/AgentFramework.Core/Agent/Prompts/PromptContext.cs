using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Prompts;

public sealed record PromptContext : IAgentPromptLayer, IStepPromptLayer
{
    public RoleDefinition?      Role       { get; init; }
    public string?              RolePrompt => Role?.ToPromptString();
    public IReadOnlyList<Skill> Skills     { get; init; } = [];

    public static PromptContext Empty => new();

    RoleDefinition? IAgentPromptLayer.Role => Role;

    IStepPromptLayer IAgentPromptLayer.WithRole(RoleDefinition role)
        => this with { Role = role };

    IStepPromptLayer IStepPromptLayer.WithSkills(IReadOnlyList<Skill> skills)
        => this with { Skills = skills };
}