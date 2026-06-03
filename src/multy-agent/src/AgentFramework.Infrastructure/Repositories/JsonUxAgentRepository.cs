using System.Text.Json;
using AgentFramework.CodePipeline;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Infrastructure.Repositories;

/// <summary>
/// Loads the UX agent configuration from a JSON file at construction time.
/// The file is the persistence-layer equivalent of <c>UxAgentDefaults</c>.
///
/// Usage:
/// <code>
/// var repo   = new JsonUxAgentRepository("TestData/ux-agent-config.json");
/// var config = await repo.GetConfigAsync();
/// var agent  = await UxAgent.BuildAsync(config!, resolver, repo);
/// </code>
/// </summary>
public sealed class JsonUxAgentRepository : IUxAgentRepository
{
    private static readonly JsonSerializerOptions Options =
        new() { PropertyNameCaseInsensitive = true };

    private readonly AgentConfig _config;
    private readonly IReadOnlyDictionary<string, string> _handlerInstructions;

    public JsonUxAgentRepository(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var dto  = JsonSerializer.Deserialize<UxAgentConfigDto>(json, Options)
                   ?? throw new InvalidOperationException(
                       $"Failed to deserialize UX agent config from '{filePath}'.");
        _config              = dto.ToDomain();
        _handlerInstructions = dto.HandlerInstructions
                               ?? new Dictionary<string, string>();
    }

    public Task<AgentConfig?> GetConfigAsync(CancellationToken ct = default)
        => Task.FromResult<AgentConfig?>(_config);

    public string? GetHandlerInstruction(string handlerName)
        => _handlerInstructions.GetValueOrDefault(handlerName);
}
