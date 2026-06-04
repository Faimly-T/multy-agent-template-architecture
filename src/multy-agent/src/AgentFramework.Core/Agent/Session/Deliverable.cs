namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// An output artifact planned or produced during the Distill or Express step.
///
/// The agent does not write the artifact — it plans what to write by recording the
/// <see cref="Path"/> where the artifact should land and the <see cref="Purpose"/> that
/// describes what the artifact must express. The domain-specific
/// <see cref="Ports.IDeliverableWriter"/> uses this record to locate and produce the actual file.
///
/// <see cref="IslandIds"/> and <see cref="DecisionIds"/> form the lineage chain:
/// they record which Islands this deliverable implements and which Decisions shaped it.
/// This makes every artifact explainable and every future change impact-assessable:
/// when a new Island is added to a Group, call <see cref="BrainAggregate.FindDeliverablesByIsland"/>
/// to discover which Deliverables may need updating.
/// </summary>
public record Deliverable(
    string                 DeliverableId,
    string                 Path,
    DeliverableStatus      Status,
    string?                GroupId     = null,
    string?                Purpose     = null,
    IReadOnlyList<string>? IslandIds   = null,
    IReadOnlyList<string>? DecisionIds = null);
