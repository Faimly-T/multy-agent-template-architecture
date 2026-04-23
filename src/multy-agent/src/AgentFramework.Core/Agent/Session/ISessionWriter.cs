using AgentFramework.Core.Agent.Steps.CODESteps;

namespace AgentFramework.Core.Agent.Session;

public interface ISessionWriter
{
    // Rehydrate phase
    void UpdateObjective(string sessionObjective);

    // Capture phase — batch
    void SetCapturedIslands(IReadOnlyList<CapturedIsland> islands);

    // Organize phase — batch
    void ApplyOrganization(IReadOnlyList<IslandOrganization> organizations, IReadOnlyList<DecisionRecord> decisions);

    // Distill phase — batch
    void ApplyDistillation(IReadOnlyList<IslandDistillation> distillations, IReadOnlyList<DeliverableRecord> deliverables);

    // Express phase
    void UpdateTokenConsumption(int inputTokens, int outputTokens);
    void RaiseQuestion(string id, string text, string source);
    void ReviewQuestion(string id, QuestionStatus newStatus);
}
