namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// Lifecycle state of an <see cref="Island"/> as it moves through the CODE pipeline.
///
/// Valid transitions (enforced by <see cref="IslandBacklog.ValidateTransition"/>):
/// <code>
///   Captured ──→ Organized ──→ Distilled
///      │               │
///      └──→ Discarded  └──→ Discarded
/// </code>
/// An island may only move forward — no reversals allowed.
/// </summary>
public enum IslandStatus
{
    /// <summary>Raw insight just added during the Capture step. Not yet grouped or acted on.</summary>
    Captured,

    /// <summary>Assigned to a semantic group during the Organize step. Ready for the Distill step.</summary>
    Organized,

    /// <summary>Fully processed — the group it belongs to has produced decisions and deliverables.</summary>
    Distilled,

    /// <summary>
    /// Removed from active reasoning. Either ungrouped after Organize (no fit found)
    /// or superseded/irrelevant after Distill.
    /// </summary>
    Discarded
}
