using AgentFramework.Core.Agent.Steps.CODESteps;

namespace AgentFramework.Core.Agent.Session;

//TODO: I need to evaluate this interface and evaluate how to handle the historical information.
public interface ISessionWriter : IQuestionWriter
{
    // Rehydrate phase
    void UpdateObjective(string sessionObjective);

    // Capture phase
    void SetCapturedIslands(IReadOnlyList<CapturedIsland> islands);

    // Organize phase
    void ApplyOrganization(IReadOnlyList<IslandOrganization> organizations, IReadOnlyList<DecisionRecord> decisions);

    // Distill phase
    void ApplyDistillation(IReadOnlyList<IslandDistillation> distillations, IReadOnlyList<DeliverableRecord> deliverables);

    // Express phase
    void UpdateTokenConsumption(int inputTokens, int outputTokens);
}
