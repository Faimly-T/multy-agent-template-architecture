using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Ports;

public interface ISkillProvider
{
    Task<Skill> LoadAsync(string skillName, CancellationToken ct = default);
}
