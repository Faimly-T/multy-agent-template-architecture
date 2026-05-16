using System.Text.RegularExpressions;
using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.Rehydrate.Handlers;

internal sealed class MarkFileLoaderHandler : IRehydrateContextHandler
{
    private readonly IMarkFileReader _reader;

    public MarkFileLoaderHandler(IMarkFileReader reader) => _reader = reader;

    public async Task<HandlerExchange> HandleAsync(
        HandlerExchange? previousExchange,
        IAgentRunContext? context, ISessionWriter writer,
        IChatClient _, CancellationToken ct)
    {
        var session = context?.Session;
        if (session is null)
            return Exchange(previousExchange, "This is the first iteration of the agent. No previous session context exists.");

        var progressMd = await _reader.ReadProgressSummaryAsync(ct);
        var distillMd = await _reader.ReadDistillHistoryAsync(ct);

        if (progressMd is null && distillMd is null)
            return Exchange(previousExchange, "This is the first iteration of the agent. No previous session context exists.");

        var history = ParseHistory(progressMd, distillMd);
        session.LoadHistory(history);

        return Exchange(previousExchange, BuildOutput(history));
    }

    private HandlerExchange Exchange(HandlerExchange? previous, string output) =>
        new(GetType().Name,
            new HandlerContent(previous?.Content.Output ?? "[start]", output),
            new HandlerMetadata(previous?.Sender, IsLlmCall: false));

    private static string BuildOutput(SessionHistory history)
    {
        var lines = new List<string>
        {
            $"Previous session #{history.LastSessionIteration} ({history.LastCheckpointDate:yyyy-MM-dd}):",
            $"Focused on: {history.LastObjective}"
        };

        if (history.AccomplishedItems.Count > 0)
            lines.Add($"Accomplished: {string.Join(", ", history.AccomplishedItems.Take(3).Select(MarkSectionParser.StripMarkdown))}");
        if (history.MomentumHeading is not null)
            lines.Add($"Momentum: {history.MomentumHeading}");
        if (history.LastDistillRunSummary is not null)
            lines.Add($"Last run summary: {history.LastDistillRunSummary}");

        return string.Join('\n', lines);
    }

    private static SessionHistory ParseHistory(string? progressMd, string? distillMd)
    {
        DateTime lastDate = default;
        int lastIteration = 1;
        string lastObjective = string.Empty;
        string? momentumHeading = null;
        string? nextFocus = null;
        string? confidence = null;
        IReadOnlyList<string> accomplished = [];
        IReadOnlyList<HistoricalArtifact> artifacts = [];

        if (progressMd is not null)
        {
            var checkpoint = MarkSectionParser.ExtractTableSection(progressMd, "Last Checkpoint");
            if (checkpoint.TryGetValue("Date", out var dateStr))
                DateTime.TryParse(dateStr, out lastDate);
            if (checkpoint.TryGetValue("Session", out var sessionStr))
                lastIteration = ParseIteration(sessionStr);
            if (checkpoint.TryGetValue("Objective", out var obj))
                lastObjective = obj;

            var momentum = MarkSectionParser.ExtractTableSection(progressMd, "Momentum Direction");
            momentum.TryGetValue("Heading", out momentumHeading);
            momentum.TryGetValue("Recommended Next Focus", out nextFocus);
            momentum.TryGetValue("Confidence", out confidence);

            accomplished = MarkSectionParser.ExtractBulletSection(progressMd, "What Was Accomplished");

            var artifactRows = MarkSectionParser.ExtractTableRows(progressMd, "Artifacts Modified");
            artifacts = artifactRows
                .Where(r => r.Length >= 2)
                .Select(r => r.Length >= 3
                    ? new HistoricalArtifact(r[0], r[1], r[2])
                    : new HistoricalArtifact(System.IO.Path.GetFileName(r[0]), r[0], r[1]))
                .ToList();
        }

        string? lastRunSummary = null;
        if (distillMd is not null)
            lastRunSummary = ExtractLastRunSummary(distillMd);

        return new SessionHistory
        {
            LastCheckpointDate = lastDate,
            LastSessionIteration = lastIteration,
            LastObjective = lastObjective,
            MomentumHeading = momentumHeading,
            RecommendedNextFocus = nextFocus,
            ConfidenceLevel = confidence,
            AccomplishedItems = accomplished,
            ArtifactsModified = artifacts,
            LastDistillRunSummary = lastRunSummary
        };
    }

    private static int ParseIteration(string sessionStr)
    {
        var match = Regex.Match(sessionStr, @"#(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out var n) ? n : 1;
    }

    private static string? ExtractLastRunSummary(string distillMd)
    {
        var lines = distillMd.Split('\n');
        var lastRunStart = -1;

        for (var i = lines.Length - 1; i >= 0; i--)
        {
            if (lines[i].TrimStart().StartsWith("## Run #", StringComparison.OrdinalIgnoreCase))
            {
                lastRunStart = i + 1;
                break;
            }
        }

        if (lastRunStart < 0) return null;

        var summary = new List<string>();
        for (var i = lastRunStart; i < lines.Length && !lines[i].TrimStart().StartsWith("## "); i++)
            summary.Add(lines[i].Trim());

        var result = string.Join(' ', summary.Where(l => l.Length > 0));
        return result.Length > 0 ? result : null;
    }
}
