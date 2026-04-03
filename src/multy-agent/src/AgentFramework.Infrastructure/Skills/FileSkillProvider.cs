using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Infrastructure.Skills;

public sealed class FileSkillProvider : ISkillProvider
{
    private readonly string _basePath;

    public FileSkillProvider(string basePath)
    {
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
    }

    public async Task<Skill> LoadAsync(string skillName, CancellationToken ct = default)
    {
        var path = Path.Combine(_basePath, skillName, "SKILL.md");

        if (!File.Exists(path))
            throw new FileNotFoundException($"Skill file not found: {path}");

        var markdown = await File.ReadAllTextAsync(path, ct);
        return Skill.FromMd(markdown);
    }
}
