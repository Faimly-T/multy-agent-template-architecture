using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Agent.Steps.CODESteps;

namespace AgentFramework.Core.Agent.Events;

public record QuestionsUpdated(
    IReadOnlyList<QuestionRecord> NewQuestions,
    int ReviewedCount) : DomainEvent;
