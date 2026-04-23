namespace AgentFramework.Core.Agent.Session;

public record Island(
    string Id,
    IslandType Type,
    string Description,
    string Source,
    string? RelatesToIslandId,
    IslandStatus Status)
{
    public Island(string id, IslandType type, string description, string source, string? relatesToIslandId = null)
        : this(id, type, description, source, relatesToIslandId, IslandStatus.Captured) { }

    public Island WithStatus(IslandStatus newStatus) => this with { Status = newStatus };
}
