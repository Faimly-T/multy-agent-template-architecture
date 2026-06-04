namespace AgentFramework.Core.Agent.Session;

// ── Capture ──────────────────────────────────────────────────────────────────

public record CapturedIsland(
    string     Id,
    IslandType Type,
    string     Description,
    string     Source,
    string?    RelatesToIslandId = null);

// ── Organize ─────────────────────────────────────────────────────────────────

public record IslandOrganization(string IslandId, IslandStatus NewStatus, string? GroupId = null);

public record IslandGroup(
    string                Id,
    string                Name,
    IReadOnlyList<string> IslandIds,
    string                WhyTogether,
    string                Readiness,
    string?               ReadinessNotes = null);

public record DecisionRecord(string Id, string Description, string Impact, string? IslandId = null);

// ── Distill ───────────────────────────────────────────────────────────────────

public record IslandDistillation(string IslandId, IslandStatus NewStatus);

// ── Lineage ───────────────────────────────────────────────────────────────────

/// <summary>
/// A BrainLineage traces everything the agent knew about a group —
/// from raw ideas (Islands) to the choices made (Decisions)
/// to the artifacts produced (Deliverables).
///
/// This is a read model: assembled on demand by <see cref="BrainAggregate.GetLineageForGroup"/>,
/// never stored. Use it to answer: "why was this deliverable built the way it was?"
/// and "if I add a new island to this group, what deliverables are at risk?"
/// </summary>
public record BrainLineage(
    IslandGroup                   Group,
    IReadOnlyList<Island>         Islands,
    IReadOnlyList<Decision>       Decisions,
    IReadOnlyList<Deliverable>    Deliverables);

// ── Serialization ─────────────────────────────────────────────────────────────

/// <summary>
/// A flat, serialization-friendly snapshot of a <see cref="BrainAggregate"/>.
/// Used by <see cref="AgentFramework.Core.Agent.Ports.IBrainRepository"/> to persist and restore the Brain.
/// </summary>
public record BrainState(
    string                         ProjectId,
    IReadOnlyList<Island>          Islands,
    IReadOnlyList<IslandGroup>     Groups,
    IReadOnlyList<Decision>        Decisions,
    IReadOnlyList<Deliverable>     Deliverables,
    IReadOnlyList<BrainCheckpoint> Checkpoints);
