using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent;

public interface IAgentRunContext
{
    AgentSession? Session { get; }
    IReadOnlyList<Question> Questions { get; }
    IReadOnlyList<Decision> Decisions { get; }
    IReadOnlyList<Deliverable> Deliverables { get; }
}
