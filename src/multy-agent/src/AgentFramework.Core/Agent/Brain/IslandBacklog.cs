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

    // --- Batch: Merge (accumulate without duplicates — used by BrainAggregate) ---

    /// <summary>
    /// Merges incoming captured islands into the backlog, skipping any with an ID that
    /// already exists. Returns the IDs of newly added islands for checkpoint tracking.
    /// </summary>
    internal IReadOnlyList<string> MergeCaptured(IReadOnlyList<CapturedIsland> incoming)
    {
        var added = new List<string>();
        foreach (var c in incoming)
        {
            if (_islands.All(i => i.Id != c.Id))
            {
                _islands.Add(new Island(c.Id, c.Type, c.Description, c.Source, c.RelatesToIslandId));
                added.Add(c.Id);
            }
        }
        return added.AsReadOnly();
    }

    // --- Restore (used by BrainAggregate.FromState — bypasses merge/validate) ---

    internal void Restore(IReadOnlyList<Island> islands)
    {
        _islands.Clear();
        _islands.AddRange(islands);
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
            _islands[index] = island with { Status = newStatus, GroupId = org.GroupId ?? island.GroupId };
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
