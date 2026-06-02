namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// Write interface for tracking session-level deliverable artifacts.
/// Deliverables are the outputs produced by the agent (documents, files, artifacts).
/// They are separate from the brain's reasoning state (<see cref="AgentBrain"/>).
/// </summary>
public interface IDeliverableTracker
{
    /// <summary>Records deliverables produced during the Distill step.</summary>
    void TrackDeliverables(
        IReadOnlyList<DeliverableRecord>       deliverables,
        IReadOnlyList<GroupDistillationRecord> groupDistillations);
}
