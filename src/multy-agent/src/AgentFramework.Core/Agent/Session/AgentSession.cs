namespace AgentFramework.Core.Agent.Session;

public class AgentSession
{
    public string ProjectId { get; private set; }

    private readonly List<Checkpoint> _checkpoints = [];
    public IReadOnlyList<Checkpoint> Checkpoints => _checkpoints.AsReadOnly();
    public Checkpoint? CurrentCheckpoint => _checkpoints.Count > 0 ? _checkpoints[^1] : null;

    /// <summary>
    /// The agent's reasoning state — all captured islands, semantic groups, and decisions
    /// made throughout the pipeline run. See <see cref="AgentBrain"/> for details.
    /// </summary>
    public AgentBrain Brain { get; } = new();

    // Convenience delegates into Brain for handlers that read islands/groups directly off the session.
    public IReadOnlyList<Island>      Islands => Brain.Backlog.All;
    public IslandBacklog              Backlog => Brain.Backlog;
    public IReadOnlyList<IslandGroup> Groups  => Brain.Groups;

    public AgentSession(string projectId)
    {
        ProjectId = projectId;
    }

    // --- Iteration lifecycle ---

    internal Checkpoint BeginIteration(string sessionObjective, string userIntent)
    {
        var cp = new Checkpoint(
            SessionIteration:  _checkpoints.Count + 1,
            CreatedAt:         DateTime.UtcNow,
            UserIntent:        userIntent,
            SessionObjective:  sessionObjective,
            TokensConsumption: new TokenConsumption(0, 0));
        _checkpoints.Add(cp);
        return cp;
    }

    internal void UpdateTokenConsumption(int inputTokens, int outputTokens)
    {
        if (_checkpoints.Count == 0) return;
        _checkpoints[^1] = _checkpoints[^1] with
        {
            TokensConsumption = new TokenConsumption(inputTokens, outputTokens)
        };
    }

    internal void FinalizeSession(DateTime closedAt)
    {
        if (_checkpoints.Count == 0) return;
        _checkpoints[^1] = _checkpoints[^1] with { ClosedAt = closedAt };
    }
}
