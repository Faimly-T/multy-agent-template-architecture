namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// Completeness state of a <see cref="Deliverable"/> produced during the Distill or Express step.
/// The LLM assigns this status when it plans the deliverable; the Express step may upgrade it
/// once the artifact is fully written.
/// </summary>
public enum DeliverableStatus
{
    /// <summary>Structure and intent defined; content not yet fully generated.</summary>
    Draft,

    /// <summary>Content exists but is incomplete — e.g. a section is missing or provisional.</summary>
    Partial,

    /// <summary>Artifact is fully generated and ready for handoff or downstream use.</summary>
    Complete
}
