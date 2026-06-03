using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Infrastructure.Skills;

public sealed class FileSkillResolver : ISkillResolver
{
    private readonly string _basePath;

    public FileSkillResolver(string basePath)
    {
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
    }

    public async Task<IReadOnlyList<SkillSummary>> GetCatalogAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_basePath)) return [];

        var summaries = new List<SkillSummary>();
        foreach (var dir in Directory.GetDirectories(_basePath))
        {
            var skillFile = Path.Combine(dir, "SKILL.md");
            if (!File.Exists(skillFile)) continue;
            var markdown = await File.ReadAllTextAsync(skillFile, ct);
            summaries.Add(SkillParser.ParseSummaryFromMarkdown(markdown));
        }
        return summaries.AsReadOnly();
    }

    public async Task<IReadOnlyList<Skill>> ResolveAsync(IReadOnlyList<string> skillNames, CancellationToken ct = default)
    {
        var skills = new List<Skill>();
        foreach (var name in skillNames)
        {
            var path = Path.Combine(_basePath, name, "SKILL.md");
            if (!File.Exists(path)) continue;
            var markdown = await File.ReadAllTextAsync(path, ct);
            skills.Add(SkillParser.ParseFromMarkdown(markdown));
        }
        return skills.AsReadOnly();
    }
}
