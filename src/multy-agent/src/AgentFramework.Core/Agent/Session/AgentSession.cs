using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Session;

public class AgentSession
{
    public Checkpoint Checkpoint { get; private set; }

    private readonly List<Island> _islands = [];
    public IReadOnlyList<Island> Islands => _islands.AsReadOnly();

    private readonly List<Deliverable> _deliverables = [];
    public IReadOnlyList<Deliverable> Deliverables => _deliverables.AsReadOnly();

    private readonly List<Decision> _decisions = [];
    public IReadOnlyList<Decision> Decisions => _decisions.AsReadOnly();

    public AgentSession(string sessionObjective, int sessionIteration = 1)
    {
        Checkpoint = new Checkpoint(
            Date: DateTime.UtcNow,
            SessionIteration: sessionIteration,
            SessionObjective: sessionObjective,
            TokensConsumption: new TokenConsumption(0, 0));
    }

    // --- Apply typed step results ---

    public void Apply(StepResult result)
    {
        switch (result)
        {
            case RehydrateResult r:
                ApplyRehydrate(r);
                break;
            case CaptureResult c:
                ApplyCapture(c);
                break;
            case OrganizeResult o:
                ApplyOrganize(o);
                break;
            case DistillResult d:
                ApplyDistill(d);
                break;
            case RelayResult relay:
                ApplyRelay(relay);
                break;
        }
    }

    private void ApplyRehydrate(RehydrateResult result)
    {
        Checkpoint = Checkpoint with { SessionObjective = result.SessionObjective };
    }

    private void ApplyCapture(CaptureResult result)
    {
        foreach (var island in result.Islands)
        {
            var captured = CaptureIsland(island.Id, island.Type, island.Description, island.Source);
            if (island.RelatesToIslandId is not null)
                captured.RelateTo(island.RelatesToIslandId);
        }
    }

    private void ApplyOrganize(OrganizeResult result)
    {
        foreach (var org in result.OrganizedIslands)
        {
            var island = FindIsland(org.IslandId);
            if (island is null) continue;

            switch (org.NewStatus)
            {
                case IslandStatus.Organized: island.Organize(); break;
                case IslandStatus.Discarded: island.Discard(); break;
            }
        }

        foreach (var dec in result.Decisions)
            RecordDecision(dec.Id, dec.Description, dec.Impact);
    }

    private void ApplyDistill(DistillResult result)
    {
        foreach (var dist in result.DistilledIslands)
        {
            var island = FindIsland(dist.IslandId);
            if (island is null) continue;

            switch (dist.NewStatus)
            {
                case IslandStatus.Distilled: island.Distill(); break;
                case IslandStatus.Discarded: island.Discard(); break;
            }
        }

        foreach (var del in result.Deliverables)
            RecordDeliverable(del.DeliverableId, del.Path, del.Status);
    }

    private void ApplyRelay(RelayResult result)
    {
        UpdateTokenConsumption(result.InputTokens, result.OutputTokens);
    }

    // --- Checkpoint ---

    public void UpdateTokenConsumption(int inputTokens, int outputTokens)
    {
        Checkpoint = Checkpoint with
        {
            TokensConsumption = new TokenConsumption(inputTokens, outputTokens)
        };
    }

    // --- Islands ---

    public Island CaptureIsland(string id, IslandType type, string description, string source)
    {
        var island = new Island(id, type, description, source);
        _islands.Add(island);
        return island;
    }

    public Island? FindIsland(string id) => _islands.Find(i => i.Id == id);

    // --- Deliverables ---

    public void RecordDeliverable(string deliverableId, string path, DeliverableStatus status)
    {
        _deliverables.Add(new Deliverable(deliverableId, path, status));
    }

    // --- Decisions ---

    public void RecordDecision(string id, string description, string impact)
    {
        _decisions.Add(new Decision(id, description, impact));
    }
}
