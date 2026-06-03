using AgentFramework.CodePipeline;
using AgentFramework.Core.Agent.Ports;

namespace AgentFramework.Domain.UxAgent;

/// <summary>
/// Domain port for loading the UX agent configuration and resolving handler instruction overrides.
/// Implement in infrastructure (e.g. <c>JsonUxAgentRepository</c>) to supply config from the
/// persistence layer without changing the agent builder or handler code.
/// </summary>
public interface IUxAgentRepository : IAgentRepository<AgentConfig> { }
