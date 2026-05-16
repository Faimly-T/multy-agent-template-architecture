using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Agent.Events;

namespace AgentFramework.Core.Agent;

public record AgentRunResult(
    AgentSession Session,
    IReadOnlyList<StepResult> StepResults,
    IReadOnlyList<DomainEvent> DomainEvents,
    bool Completed,
    IReadOnlyList<Question> Questions,
    IReadOnlyList<Decision> Decisions,
    IReadOnlyList<Deliverable> Deliverables);
