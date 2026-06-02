namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// An output artifact planned or produced during the Distill or Express step.
///
/// The agent does not write the artifact — it plans what to write by recording the
/// <see cref="Path"/> where the artifact should land and the <see cref="Purpose"/> that
/// describes what the artifact must express. The domain-specific
/// <see cref="Ports.IDeliverableWriter"/> uses this record to locate and produce the actual file.
/// </summary>
public record Deliverable(
    string            DeliverableId,
    string            Path,
    DeliverableStatus Status,
    string?           GroupId = null,
    string?           Purpose = null);
