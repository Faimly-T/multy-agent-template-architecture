using AgentFramework.Core.Agent.Prompts;

namespace AgentFramework.Core.Agent.Ports;

public interface IRoleLoader
{
    Task<RoleDefinition> LoadAsync(string roleName, CancellationToken ct = default);
}
