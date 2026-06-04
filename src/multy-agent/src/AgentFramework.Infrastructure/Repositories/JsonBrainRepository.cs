using System.Text.Json;
using System.Text.Json.Serialization;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Infrastructure.Repositories;

/// <summary>
/// Persists a <see cref="BrainAggregate"/> as a JSON file on the local filesystem.
///
/// Files are stored at: <c>{marksPath}/{projectId}_Brain.json</c>
/// This integrates with the existing MARK file directory used by agent sessions.
///
/// Usage:
/// <code>
/// var brainRepo = new JsonBrainRepository("outputs/contextAgent");
/// var brain     = await brainRepo.GetAsync(config.ProjectId, ct)
///                 ?? new BrainAggregate(config.ProjectId);
/// var agent     = await UxAgent.BuildAsync(config, resolver, brain: brain);
/// // ... run agent ...
/// await brainRepo.SaveAsync(agent.Brain, ct);
/// </code>
/// </summary>
public sealed class JsonBrainRepository : IBrainRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented               = true,
        Converters                  = { new JsonStringEnumConverter() }
    };

    private readonly string _marksPath;

    public JsonBrainRepository(string marksPath)
    {
        _marksPath = marksPath;
    }

    public async Task<BrainAggregate?> GetAsync(BrainId id, CancellationToken ct = default)
    {
        var path = BuildPath(id);
        if (!File.Exists(path)) return null;

        var json  = await File.ReadAllTextAsync(path, ct);
        var state = JsonSerializer.Deserialize<BrainState>(json, Options);
        return state is null ? null : BrainAggregate.FromState(state);
    }

    public async Task SaveAsync(BrainAggregate brain, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_marksPath);
        var path  = BuildPath(brain.Id);
        var state = brain.ToState();
        var json  = JsonSerializer.Serialize(state, Options);
        await File.WriteAllTextAsync(path, json, ct);
    }

    public Task<bool> ExistsAsync(BrainId id, CancellationToken ct = default)
        => Task.FromResult(File.Exists(BuildPath(id)));

    private string BuildPath(BrainId id)
        => Path.Combine(_marksPath, $"{id.ProjectId}_Brain.json");
}
