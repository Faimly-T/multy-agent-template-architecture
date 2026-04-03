namespace AgentFramework.Core.Agent.Ports;

public interface ISkillProvider
{
    Task<Steps.Skill> LoadAsync(string skillName, CancellationToken ct = default);
}
