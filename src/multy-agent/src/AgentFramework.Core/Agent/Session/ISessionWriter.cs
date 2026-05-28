using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Steps.CODESteps;

namespace AgentFramework.Core.Agent.Session;

public interface ISessionWriter : IQuestionWriter
{
    // Called by KickoffResult to create the first checkpoint of the session
    void BeginIteration(string sessionObjective);

    // Kickoff phase
    void UpdateObjective(string sessionObjective);

    // Per-step exchange recording
    void RecordStepExchange(int stepNumber, string stepName,
        IReadOnlyList<ChatMessage> messages, string response);

    // Capture phase
    void SetCapturedIslands(IReadOnlyList<CapturedIsland> islands);

    // Organize phase
    void ApplyOrganization(IReadOnlyList<IslandOrganization> organizations, IReadOnlyList<DecisionRecord> decisions);

    // Distill phase
    void ApplyDistillation(IReadOnlyList<IslandDistillation> distillations, IReadOnlyList<DeliverableRecord> deliverables);

    // Express phase
    void UpdateTokenConsumption(int inputTokens, int outputTokens);
}
