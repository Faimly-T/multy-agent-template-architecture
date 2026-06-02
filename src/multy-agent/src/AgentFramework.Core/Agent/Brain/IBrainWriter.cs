namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// Write interface for the agent's <see cref="AgentBrain"/>.
/// Covers the three reasoning phases that mutate brain state:
/// Capture (islands), Organize (groups + decisions), Distill (island status + group decisions).
/// </summary>
public interface IBrainWriter
{
    /// <summary>Capture phase — replace the island backlog with the newly captured set.</summary>
    void SetCapturedIslands(IReadOnlyList<CapturedIsland> islands);

    /// <summary>Organize phase — record groups, organizational decisions, and update island statuses.</summary>
    void ApplyOrganization(
        IReadOnlyList<IslandOrganization> organizations,
        IReadOnlyList<DecisionRecord>     decisions,
        IReadOnlyList<IslandGroup>        groups);

    /// <summary>Distill phase — advance island statuses and record group-level decisions.</summary>
    void ApplyDistillation(
        IReadOnlyList<IslandDistillation>    distillations,
        IReadOnlyList<GroupDistillationRecord> groupDistillations);
}
