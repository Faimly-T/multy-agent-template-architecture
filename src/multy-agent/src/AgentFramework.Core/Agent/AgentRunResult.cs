using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent;

public record AgentRunResult(
    AgentSession Session,
    IReadOnlyList<StepResult> StepResults,
    bool Completed,
    IReadOnlyList<Question> Questions,
    IReadOnlyList<Decision> Decisions,
    IReadOnlyList<Deliverable> Deliverables);
