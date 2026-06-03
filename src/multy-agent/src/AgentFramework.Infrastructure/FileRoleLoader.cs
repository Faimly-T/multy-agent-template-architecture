using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;

namespace AgentFramework.Infrastructure;

public sealed class FileRoleLoader : IRoleLoader
{
    private readonly string _basePath;

    public FileRoleLoader(string basePath)
    {
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
    }

    public async Task<RoleDefinition> LoadAsync(string roleName, CancellationToken ct = default)
    {
        var path = Path.Combine(_basePath, $"{roleName}.md");

        if (!File.Exists(path))
            throw new FileNotFoundException($"Role file not found: {path}", path);

        var markdown = await File.ReadAllTextAsync(path, ct);
        return RoleParser.ParseFromMarkdown(markdown);
    }
}
