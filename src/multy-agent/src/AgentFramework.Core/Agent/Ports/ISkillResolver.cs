using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Ports;

public interface ISkillResolver
{
    Task<IReadOnlyList<SkillSummary>> GetCatalogAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Skill>> ResolveAsync(IReadOnlyList<string> skillNames, CancellationToken ct = default);
}
