using System.Text.Json;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Infrastructure.Anthropic;

internal static class StepResultParser
{
    public static StepResult Parse(string json, string rawOutput, AgentStep step)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var gateSatisfied = root.TryGetProperty("gateSatisfied", out var g) && g.GetBoolean();

        return step.ParseResult(root, rawOutput, gateSatisfied);
    }
}
