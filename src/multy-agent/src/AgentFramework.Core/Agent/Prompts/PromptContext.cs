using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Prompts;

/// <summary>
/// Immutable context record that carries a role and a set of skills into a pipeline step.
/// Implements <see cref="IStepPromptLayer"/> — the single interface steps and handlers use to access
/// prompt material.
///
/// Build order:
/// <list type="number">
///   <item><see cref="ForRole"/> — seed with the agent's <see cref="RoleDefinition"/>.</item>
///   <item><see cref="IStepPromptLayer.WithSkills"/> — layer in step-specific skills (called by <c>AgentBuilder</c> during pipeline assembly).</item>
/// </list>
/// </summary>
public sealed record PromptContext : IStepPromptLayer
{
    public RoleDefinition?      Role       { get; init; }
    public string?              RolePrompt => Role?.ToPromptString();
    public IReadOnlyList<Skill> Skills     { get; init; } = [];

    /// <summary>An empty context — no role, no skills. Use as a starting point or in tests.</summary>
    public static PromptContext Empty => new();

    /// <summary>Creates a prompt context seeded with the agent role. Call <see cref="IStepPromptLayer.WithSkills"/> on the result to add step-specific skills.</summary>
    public static PromptContext ForRole(RoleDefinition role) => Empty with { Role = role };

    IStepPromptLayer IStepPromptLayer.WithSkills(IReadOnlyList<Skill> skills)
        => this with { Skills = skills };
}
