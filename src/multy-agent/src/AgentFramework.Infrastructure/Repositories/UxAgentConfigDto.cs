using AgentFramework.CodePipeline;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Infrastructure.Repositories;

/// <summary>
/// JSON-serializable representation of <see cref="AgentConfig"/> for the UX agent.
/// Mirrors the data in <c>UxAgentDefaults</c>; the repository deserializes this DTO
/// and maps it to the domain record via <see cref="ToDomain"/>.
/// </summary>
public record UxAgentConfigDto(
    string            AgentId,
    string            ProjectId,
    RoleDefinitionDto Role,
    StepConfigDto     Kickoff,
    StepConfigDto     Capture,
    StepConfigDto     Organize,
    StepConfigDto     Distill,
    StepConfigDto     Express,
    StepConfigDto?    Closed,
    Dictionary<string, string>? HandlerInstructions)
{
    public AgentConfig ToDomain() => new(
        AgentId, ProjectId,
        Role.ToDomain(),
        Kickoff.ToDomain(),
        Capture.ToDomain(),
        Organize.ToDomain(),
        Distill.ToDomain(),
        Express.ToDomain(),
        Closed?.ToDomain());
}

public record RoleDefinitionDto(
    string      Name,
    string      Description,
    IdentityDto Identity,
    string      Mandate,
    string[]    FactsAndDirectives)
{
    public RoleDefinition ToDomain() => new(
        Name, Description,
        new Identity(Identity.Role, Identity.Persona, Identity.Authority, Identity.Boundary),
        Mandate,
        FactsAndDirectives);
}

public record IdentityDto(string Role, string Persona, string Authority, string Boundary);

public record StepConfigDto(string Instructions, string Gate, string[] SkillNames)
{
    public StepConfig ToDomain() => new(Instructions, new Gate(Gate), SkillNames);
}
