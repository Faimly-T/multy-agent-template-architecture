namespace AgentFramework.Core.Agent.Session;

public class Island
{
    public string Id { get; }
    public IslandType Type { get; }
    public string Description { get; }
    public string Source { get; }
    public string? RelatesToIslandId { get; private set; }
    public IslandStatus Status { get; private set; }

    public Island(string id, IslandType type, string description, string source)
    {
        Id = id;
        Type = type;
        Description = description;
        Source = source;
        Status = IslandStatus.Captured;
    }

    public void RelateTo(string islandId) => RelatesToIslandId = islandId;

    public void Organize() => Status = IslandStatus.Organized;

    public void Distill() => Status = IslandStatus.Distilled;

    public void Discard() => Status = IslandStatus.Discarded;
}
