namespace AgentFramework.Core.Agent.Session;

public class SessionHistory
{
    public DateTime LastCheckpointDate { get; init; }
    public int LastSessionIteration { get; init; }
    public string LastObjective { get; init; } = string.Empty;
    public string? MomentumHeading { get; init; }
    public string? RecommendedNextFocus { get; init; }
    public string? ConfidenceLevel { get; init; }
    public IReadOnlyList<string> AccomplishedItems { get; init; } = [];
    public IReadOnlyList<HistoricalArtifact> ArtifactsModified { get; init; } = [];
    public IReadOnlyList<string> DecisionIds { get; init; } = [];
    public string? LastDistillRunSummary { get; init; }
}

public record HistoricalArtifact(string Name, string Path, string ChangeType);
