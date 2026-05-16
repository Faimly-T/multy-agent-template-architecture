namespace AgentFramework.Core.Agent.Session;

public class AgentSession
{
    public string ProjectId { get; private set; }
    public SessionMarkFilePaths MarkFilePaths { get; private set; }

    private readonly List<Checkpoint> _checkpoints = [];
    public IReadOnlyList<Checkpoint> Checkpoints => _checkpoints.AsReadOnly();
    public Checkpoint? CurrentCheckpoint => _checkpoints.Count > 0 ? _checkpoints[^1] : null;

    public IslandBacklog Backlog { get; } = new();
    public IReadOnlyList<Island> Islands => Backlog.All;

    public SessionHistory? History { get; private set; }

    public AgentSession(string projectId, SessionMarkFilePaths markFilePaths)
    {
        ProjectId = projectId;
        MarkFilePaths = markFilePaths;
    }

    // --- Iteration lifecycle ---

    internal Checkpoint BeginIteration(string objective)
    {
        var cp = new Checkpoint(
            Date: DateTime.UtcNow,
            SessionIteration: _checkpoints.Count + 1,
            SessionObjective: objective,
            TokensConsumption: new TokenConsumption(0, 0));
        _checkpoints.Add(cp);
        return cp;
    }

    internal void UpdateObjective(string sessionObjective)
    {
        if (_checkpoints.Count == 0) return;
        _checkpoints[^1] = _checkpoints[^1] with { SessionObjective = sessionObjective };
    }

    internal void UpdateTokenConsumption(int inputTokens, int outputTokens)
    {
        if (_checkpoints.Count == 0) return;
        _checkpoints[^1] = _checkpoints[^1] with
        {
            TokensConsumption = new TokenConsumption(inputTokens, outputTokens)
        };
    }

    // --- History ---

    internal void LoadHistory(SessionHistory history) => History = history;
}
