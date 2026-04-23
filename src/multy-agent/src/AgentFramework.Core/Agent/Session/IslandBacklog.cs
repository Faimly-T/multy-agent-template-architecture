using AgentFramework.Core.Agent.Steps.CODESteps;

namespace AgentFramework.Core.Agent.Session;

public sealed class IslandBacklog
{
    private readonly List<Island> _islands = [];

    public IReadOnlyList<Island> All => _islands.AsReadOnly();
    public int Count => _islands.Count;

    public IReadOnlyList<Island> GetByStatus(IslandStatus status)
        => _islands.Where(i => i.Status == status).ToList().AsReadOnly();

    public Island? Find(string id) => _islands.Find(i => i.Id == id);

    // --- Batch: Capture (replaces entire backlog) ---

    internal void SetCaptured(IReadOnlyList<CapturedIsland> islands)
    {
        ValidateNoDuplicateIds(islands.Select(i => i.Id));

        _islands.Clear();
        foreach (var captured in islands)
        {
            _islands.Add(new Island(
                captured.Id,
                captured.Type,
                captured.Description,
                captured.Source,
                captured.RelatesToIslandId));
        }
    }

    // --- Batch: Organize ---

    internal void ApplyOrganization(IReadOnlyList<IslandOrganization> organizations)
    {
        foreach (var org in organizations)
        {
            var index = FindIndex(org.IslandId);
            var island = _islands[index];

            var newStatus = org.NewStatus;
            ValidateTransition(island, newStatus);
            _islands[index] = island.WithStatus(newStatus);
        }

        if (!_islands.Any(i => i.Status == IslandStatus.Organized))
            throw new InvalidOperationException("Organization must keep at least one island as Organized.");
    }

    // --- Batch: Distill ---

    internal void ApplyDistillation(IReadOnlyList<IslandDistillation> distillations)
    {
        foreach (var dist in distillations)
        {
            var index = FindIndex(dist.IslandId);
            var island = _islands[index];

            var newStatus = dist.NewStatus;
            ValidateTransition(island, newStatus);
            _islands[index] = island.WithStatus(newStatus);
        }
    }

    // --- Transition Validation ---

    private static void ValidateTransition(Island island, IslandStatus newStatus)
    {
        var valid = (island.Status, newStatus) switch
        {
            (IslandStatus.Captured, IslandStatus.Organized) => true,
            (IslandStatus.Captured, IslandStatus.Discarded) => true,
            (IslandStatus.Organized, IslandStatus.Distilled) => true,
            (IslandStatus.Organized, IslandStatus.Discarded) => true,
            _ => false
        };

        if (!valid)
            throw new InvalidOperationException(
                $"Cannot transition island '{island.Id}' from '{island.Status}' to '{newStatus}'.");
    }

    private int FindIndex(string islandId)
    {
        var index = _islands.FindIndex(i => i.Id == islandId);
        if (index < 0)
            throw new InvalidOperationException($"Island '{islandId}' not found.");
        return index;
    }

    private static void ValidateNoDuplicateIds(IEnumerable<string> ids)
    {
        var seen = new HashSet<string>();
        foreach (var id in ids)
        {
            if (!seen.Add(id))
                throw new InvalidOperationException($"Duplicate island ID '{id}' in capture batch.");
        }
    }
}
