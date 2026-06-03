namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// The agent's reasoning state — the "brain" of each run.
///
/// Encapsulates the full thinking process across the CODE pipeline:
/// <list type="bullet">
///   <item><b>Islands</b> — captured ideas/observations about the problem (Capture step).</item>
///   <item><b>Groups</b>  — islands clustered by shared concern (Organize step).</item>
///   <item><b>Decisions</b> — choices made over islands/groups during reasoning (Organize + Distill).</item>
/// </list>
///
/// The brain is a first-class entity owned by <see cref="AgentSession"/>. The aggregate
/// writes to it via <see cref="IBrainWriter"/>; handlers read from it via
/// <see cref="Core.Agent.IAgentRunContext.Session"/>.
/// </summary>
public sealed class AgentBrain
{
    private readonly IslandBacklog     _backlog   = new();
    private readonly List<IslandGroup> _groups    = [];
    private readonly List<Decision>    _decisions = [];

    // ── Read access ──────────────────────────────────────────────────────────

    /// <summary>All captured islands with their current status in the reasoning pipeline.</summary>
    public IslandBacklog Backlog => _backlog;

    /// <summary>Semantic groups formed during the Organize step.</summary>
    public IReadOnlyList<IslandGroup> Groups => _groups.AsReadOnly();

    /// <summary>All decisions taken over islands and groups throughout the run.</summary>
    public IReadOnlyList<Decision> Decisions => _decisions.AsReadOnly();

    // ── Mutation (internal — only IBrainWriter implementations may call these) ──

    internal void SetCapturedIslands(IReadOnlyList<CapturedIsland> islands)
        => _backlog.SetCaptured(islands);

    internal void ApplyOrganization(
        IReadOnlyList<IslandOrganization> organizations,
        IReadOnlyList<DecisionRecord>     decisions,
        IReadOnlyList<IslandGroup>        groups)
    {
        _backlog.ApplyOrganization(organizations);
        _groups.Clear();
        _groups.AddRange(groups);
        foreach (var d in decisions)
            _decisions.Add(new Decision(d.Id, d.Description, d.Impact));
    }

    internal void ApplyDistillation(
        IReadOnlyList<IslandDistillation>    distillations,
        IReadOnlyList<GroupDistillationRecord> groupDistillations)
    {
        _backlog.ApplyDistillation(distillations);
        foreach (var grp in groupDistillations)
            foreach (var d in grp.Decisions)
                _decisions.Add(new Decision(d.Id, d.Description, d.Impact, grp.GroupId));
    }
}
