namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// An immutable audit record of what one agent contributed to the Brain during a single checkpoint.
///
/// The Brain accumulates these across all agents and sessions for a project.
/// Each record answers: "who added what, and when?"
/// </summary>
public record BrainCheckpoint(
    int       Number,
    string    AgentId,
    string    SessionObjective,
    DateTime  CreatedAt,
    DateTime? ClosedAt                          = null,
    IReadOnlyList<string>? IslandIdsAdded       = null,
    IReadOnlyList<string>? GroupIdsAdded        = null,
    IReadOnlyList<string>? DecisionIdsAdded     = null,
    IReadOnlyList<string>? DeliverableIdsAdded  = null);
