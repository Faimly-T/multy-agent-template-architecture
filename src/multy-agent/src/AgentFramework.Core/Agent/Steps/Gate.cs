using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps;

public record Gate(string Description, Func<AgentSession, StepResult, bool>? Verify = null);
