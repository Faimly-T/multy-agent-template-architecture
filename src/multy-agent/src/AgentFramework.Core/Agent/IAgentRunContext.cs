using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent;

public interface IAgentRunContext
{
    public Role Role { get; }
    AgentSession? Session { get; }
    IReadOnlyList<Question> Questions { get; }
    IReadOnlyList<Decision> Decisions { get; }
    IReadOnlyList<Deliverable> Deliverables { get; }
}
