using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Tests.TestHelpers;

/// <summary>
/// Resolves skills from flat {basePath}/{skillName}.md files.
/// For use in tests only — production code uses FileSkillResolver.
/// </summary>
internal sealed class FlatFileSkillResolver : ISkillResolver
{
    private readonly string _basePath;

    public FlatFileSkillResolver(string basePath) => _basePath = basePath;

    public Task<IReadOnlyList<SkillSummary>> GetCatalogAsync(CancellationToken ct = default)
    {
        var summaries = Directory.GetFiles(_basePath, "*.md")
            .Select(f => SkillParser.ParseSummaryFromMarkdown(File.ReadAllText(f)))
            .ToList();
        return Task.FromResult<IReadOnlyList<SkillSummary>>(summaries);
    }

    public Task<IReadOnlyList<Skill>> ResolveAsync(
        IReadOnlyList<string> skillNames, CancellationToken ct = default)
    {
        var skills = skillNames
            .Select(name => Path.Combine(_basePath, $"{name}.md"))
            .Where(File.Exists)
            .Select(path => SkillParser.ParseFromMarkdown(File.ReadAllText(path)))
            .ToList();
        return Task.FromResult<IReadOnlyList<Skill>>(skills);
    }
}
