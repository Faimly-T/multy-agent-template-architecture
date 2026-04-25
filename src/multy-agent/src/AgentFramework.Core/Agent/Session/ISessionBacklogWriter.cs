using AgentFramework.Core.Agent.Steps.CODESteps;

namespace AgentFramework.Core.Agent.Session;

public interface ISessionBacklogWriter
{
    void SetCapturedIslands(IReadOnlyList<CapturedIsland> islands);
    void ApplyOrganization(IReadOnlyList<IslandOrganization> organizations, IReadOnlyList<DecisionRecord> decisions);
    void ApplyDistillation(IReadOnlyList<IslandDistillation> distillations, IReadOnlyList<DeliverableRecord> deliverables);
}
