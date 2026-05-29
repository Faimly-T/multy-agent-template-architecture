using System.Text.Json;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain;
using AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain.Handlers;

namespace AgentFramework.Core.Agent.Steps.CODESteps;

public class KickoffStep : AgentStep
{
    private readonly IMarkFileReader _markFileReader;

    public KickoffStep(
        IStepPromptLayer   stepContext,
        int                stepNumber,
        string             instructions,
        Gate               gate,
        IMarkFileReader?   markFileReader = null)
        : base(stepContext, stepNumber, "Kickoff-context", instructions, gate)
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

    public override async Task<StepResult?> ExecuteStepAsync(
        IAgentRunContext? context,
        ISessionWriter writer,
        IChatClient chatClient,
        CancellationToken ct = default)
    {
        var chain = new KickoffContextChain(
            new CheckpointValidatorCommand(_stepContext),
            new MarkFileLoaderHandler(_markFileReader),
            new IterationEvaluatorHandler(),
            new QuestionTriageHandler(_markFileReader, _stepContext),
            new ObjectiveSynthesisHandler(_stepContext));

        var (finalJson, journal) = await chain.RunAsync(context, writer, chatClient, ct);

        Journal.Clear();
        Journal.AddRange(journal);

        // Tracking messages for ConversationMessages — role only, minimal label
        var trackingMessages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(_stepContext.RolePrompt))
            trackingMessages.Add(new ChatMessage(MessageRole.System, _stepContext.RolePrompt));
        trackingMessages.Add(new ChatMessage(MessageRole.User, $"## Step {StepNumber}: {Name}"));
        writer.RecordStepExchange(StepNumber, Name, trackingMessages, finalJson);

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

        var blockers = new List<KickoffBlocker>();
        if (root.TryGetProperty("blockers", out var bArr))
        {
            foreach (var el in bArr.EnumerateArray())
            {
                var qId      = el.GetProperty("questionId").GetString() ?? string.Empty;
                var text     = el.GetProperty("text").GetString() ?? string.Empty;
                var severity = el.GetProperty("severity").GetString() ?? "soft";
                blockers.Add(new KickoffBlocker(qId, text, severity));
            }
        }

        return new KickoffResult(rawOutput, gateSatisfied, objective, narrativeBridge,
            stalenessWarning, isInitialSession, blockers);
    }

    private sealed class NullMarkFileReader : IMarkFileReader
    {
        public Task<string?> ReadProgressSummaryAsync(CancellationToken ct) => Task.FromResult<string?>(null);
        public Task<string?> ReadQuestionsLogAsync(CancellationToken ct)    => Task.FromResult<string?>(null);
        public Task<string?> ReadDistillHistoryAsync(CancellationToken ct)  => Task.FromResult<string?>(null);
    }
}


public record KickoffBlocker(string QuestionId, string Text, string Severity);

public record KickoffResult(
    string Output,
    bool GateSatisfied,
    string SessionObjective,
    string NarrativeBridge = "",
    string? StalenessWarning = null,
    bool IsInitialSession = false,
    IReadOnlyList<KickoffBlocker>? Blockers = null) : StepResult(Output, GateSatisfied)
{
    public override void ApplyTo(ISessionWriter writer)
    {
        writer.BeginIteration(SessionObjective);
    }
}
