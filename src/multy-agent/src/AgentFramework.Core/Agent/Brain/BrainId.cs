namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// The identity of a <see cref="BrainAggregate"/>.
/// One Brain exists per project — all agents working on the same project share it.
/// </summary>
public record BrainId(string ProjectId)
{
    public override string ToString() => ProjectId;

    /// <summary>Allows implicit conversion from a plain project-id string.</summary>
    public static implicit operator BrainId(string projectId) => new(projectId);
}
