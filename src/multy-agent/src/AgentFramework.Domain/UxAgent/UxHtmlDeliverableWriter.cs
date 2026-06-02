using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Domain.UxAgent.Express;

namespace AgentFramework.Domain.UxAgent;

/// <summary>
/// Writes the five HTML documents produced by the UX Express step to an output directory.
/// Called by AgentAggregate.RunAsync after all steps complete (including ClosedStep).
/// </summary>
public sealed class UxHtmlDeliverableWriter : IDeliverableWriter
{
    private readonly string _outputDirectory;

    /// <param name="outputDirectory">
    /// Directory where the HTML files will be written.
    /// Defaults to "outputs/ux-persona" relative to the current working directory.
    /// </param>
    public UxHtmlDeliverableWriter(string? outputDirectory = null)
    {
        _outputDirectory = outputDirectory ?? Path.Combine("outputs", "ux-persona");
    }

    public async Task WriteAsync(
        IAgentRunContext          context,
        IReadOnlyList<StepResult> results,
        CancellationToken         ct = default)
    {
        var expressResult = results.OfType<UxExpressResult>().LastOrDefault();
        if (expressResult is null) return;

        Directory.CreateDirectory(_outputDirectory);

        var projectId = context.Session?.ProjectId ?? "run";
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmm");
        var prefix    = $"{projectId}_{timestamp}";

        var files = new[]
        {
            (Name: "session-summary.html",          Html: expressResult.HtmlSummary),
            (Name: "buyer-persona.html",             Html: expressResult.HtmlPersona),
            (Name: "research-validation-guide.html", Html: expressResult.HtmlResearch),
            (Name: "interview-script.html",          Html: expressResult.HtmlInterview),
            (Name: "next-session-guide.html",        Html: expressResult.HtmlNextSession),
        };

        foreach (var (name, html) in files)
        {
            if (string.IsNullOrWhiteSpace(html)) continue;
            var path = Path.Combine(_outputDirectory, $"{prefix}_{name}");
            await File.WriteAllTextAsync(path, html, System.Text.Encoding.UTF8, ct);
        }
    }

    /// <summary>Returns the paths that would be written for a given project/timestamp.</summary>
    public IReadOnlyList<string> GetExpectedPaths(string projectId)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmm");
        var prefix    = $"{projectId}_{timestamp}";
        return new[]
        {
            Path.Combine(_outputDirectory, $"{prefix}_session-summary.html"),
            Path.Combine(_outputDirectory, $"{prefix}_buyer-persona.html"),
            Path.Combine(_outputDirectory, $"{prefix}_research-validation-guide.html"),
            Path.Combine(_outputDirectory, $"{prefix}_interview-script.html"),
            Path.Combine(_outputDirectory, $"{prefix}_next-session-guide.html"),
        };
    }
}
