using System.Text.Json;
using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps.CODESteps.Rehydrate;
using AgentFramework.Core.Agent.Steps.CODESteps.Rehydrate.Handlers;

namespace AgentFramework.Core.Agent.Steps.CODESteps;

public class RehydrateStep : AgentStep
{
    private readonly IMarkFileReader _markFileReader;

    public RehydrateStep(int stepNumber, string name, string instructions, Gate gate,
        IMarkFileReader? markFileReader = null)
        : base(stepNumber, name, "rehydrate-context", instructions, gate)
    {
        _markFileReader = markFileReader ?? new NullMarkFileReader();
    }

    public override string JsonResponseSchema => """
        {
          "sessionObjective": "string — verb + deliverable + success condition + stakes clause",
          "narrativeBridge": "string — 2-3 sentences connecting last session's end to this session's start",
          "isInitialSession": false,
          "stalenessWarning": "string or null — staleness warning if checkpoint is stale",
          "blockers": [
            { "questionId": "Q-001", "text": "blocker description", "severity": "hard|soft" }
          ],
          "gateSatisfied": true
        }
        """;

    public List<HandlerExchange> Journal { get; } = [];

    public override async Task<StepResult?> ExecuteChainAsync(
        IAgentRunContext? context,
        ISessionWriter writer,
        IChatClient chatClient,
        IDomainEventPublisher? publisher = null,
        Role? role = null,
        CancellationToken ct = default)
    {
        var chain = new RehydrateContextChain(
            new MarkFileLoaderHandler(_markFileReader),
            new IterationEvaluatorHandler(),
            new QuestionTriageHandler(_markFileReader),
            new ObjectiveSynthesisHandler(role, Skill));

        var (finalJson, journal) = await chain.RunAsync(context, writer, chatClient, ct);

        Journal.Clear();
        Journal.AddRange(journal);

        if (publisher is not null)
            foreach (var exchange in journal)
                publisher.Publish(new HandlerExchanged(StepNumber, Name, exchange));

        using var doc = JsonDocument.Parse(finalJson);
        var gateSatisfied = !doc.RootElement.TryGetProperty("gateSatisfied", out var gs) || gs.GetBoolean();
        return ParseResult(doc.RootElement, finalJson, gateSatisfied);
    }

    public override StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied)
    {
        var objective = root.GetProperty("sessionObjective").GetString() ?? string.Empty;

        var narrativeBridge = root.TryGetProperty("narrativeBridge", out var nb)
            ? nb.GetString() ?? string.Empty
            : string.Empty;

        var isInitialSession = root.TryGetProperty("isInitialSession", out var init)
            && init.GetBoolean();

        var stalenessWarning = root.TryGetProperty("stalenessWarning", out var sw)
            ? sw.GetString()
            : null;

        var blockers = new List<RehydrateBlocker>();
        if (root.TryGetProperty("blockers", out var bArr))
        {
            foreach (var el in bArr.EnumerateArray())
            {
                var qId = el.GetProperty("questionId").GetString() ?? string.Empty;
                var text = el.GetProperty("text").GetString() ?? string.Empty;
                var severity = el.GetProperty("severity").GetString() ?? "soft";
                blockers.Add(new RehydrateBlocker(qId, text, severity));
            }
        }

        return new RehydrateResult(rawOutput, gateSatisfied, objective, narrativeBridge,
            stalenessWarning, isInitialSession, blockers);
    }

    private sealed class NullMarkFileReader : IMarkFileReader
    {
        public Task<string?> ReadProgressSummaryAsync(CancellationToken ct) => Task.FromResult<string?>(null);
        public Task<string?> ReadQuestionsLogAsync(CancellationToken ct) => Task.FromResult<string?>(null);
        public Task<string?> ReadDistillHistoryAsync(CancellationToken ct) => Task.FromResult<string?>(null);
    }
}

public record RehydrateBlocker(string QuestionId, string Text, string Severity);

public record RehydrateResult(
    string Output,
    bool GateSatisfied,
    string SessionObjective,
    string NarrativeBridge = "",
    string? StalenessWarning = null,
    bool IsInitialSession = false,
    IReadOnlyList<RehydrateBlocker>? Blockers = null) : StepResult(Output, GateSatisfied)
{
    public override void ApplyTo(ISessionWriter writer)
    {
        writer.UpdateObjective(SessionObjective);
    }
}
