namespace AgentFramework.Core.Agent.Session;

public class AgentSession
{
    public string ProjectId { get; private set; }

    private readonly List<Checkpoint> _checkpoints = [];
    public IReadOnlyList<Checkpoint> Checkpoints => _checkpoints.AsReadOnly();
    public Checkpoint? CurrentCheckpoint => _checkpoints.Count > 0 ? _checkpoints[^1] : null;

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
