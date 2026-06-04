using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.CodePipeline;

/// <summary>
/// Validation record produced by ClosedStep.
/// </summary>
public record AgentValidation(bool IsValid, string Summary, IReadOnlyList<string> Issues)
{
    public static AgentValidation Empty => new(true, "No issues found.", []);
}

/// <summary>
/// Step 6 — Session Close.
/// Pure code step: no LLM call.
/// Validates the aggregate state and seals the checkpoint with a ClosedAt timestamp.
/// Token totals come from the checkpoint already accumulated by the Express step.
/// </summary>
public class ClosedStep : AgentStep
{
    public ClosedStep(
        IStepPromptLayer stepContext,
        int              stepNumber,
        string           instructions,
        Gate             gate)
        : base(stepContext, stepNumber, instructions, gate) { }

    public override string JsonResponseSchema => "{}";  // no LLM call

    public override StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied)
        => new ClosedResult(rawOutput, gateSatisfied, AgentValidation.Empty, 0, 0, DateTime.UtcNow);

    public override Task<StepResult?> ExecuteStepAsync(
        IAgentRunContext? context,
        ISessionWriter    writer,
        IChatClient       chatClient,
        CancellationToken ct = default)
    {
        var validation  = ValidateAggregate(context);
        var closedAt    = DateTime.UtcNow;

        // Total tokens come from the checkpoint (accumulated by ExpressResult.ApplyTo)
        var checkpoint   = context?.Session?.CurrentCheckpoint;
        var inputTokens  = checkpoint?.TokensConsumption.InputTokens  ?? 0;
        var outputTokens = checkpoint?.TokensConsumption.OutputTokens ?? 0;

        var output =
            $"Session closed at {closedAt:O}. " +
            $"Total tokens: {inputTokens + outputTokens}. " +
            $"Validation: {validation.Summary}";

        var auditEntry = new HandlerExchange(
            nameof(ClosedStep),
            new HandlerContent(string.Empty, output),
            new HandlerMetadata(null, IsLlmCall: false));
        writer.RecordStepJournal(StepNumber, Name, [auditEntry]);

        var result = new ClosedResult(output, validation.IsValid, validation,
            inputTokens, outputTokens, closedAt);

        return Task.FromResult<StepResult?>(result);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Aggregate-state validation
    // ──────────────────────────────────────────────────────────────────────

    private static AgentValidation ValidateAggregate(IAgentRunContext? context)
    {
        if (context is null)
            return new AgentValidation(false, "No agent context.", ["Context is null."]);

        var session = context.Session;
        if (session is null)
            return new AgentValidation(false, "No session exists.", ["Session is null."]);

        var issues = new List<string>();

        if (session.CurrentCheckpoint is null)
            issues.Add("No checkpoint recorded — Kickoff may not have run.");

        var brain          = context.Brain;
        var allIslands     = brain?.Islands ?? [];
        var distilledCount = brain?.Backlog.GetByStatus(IslandStatus.Distilled).Count ?? 0;
        var discardedCount = brain?.Backlog.GetByStatus(IslandStatus.Discarded).Count ?? 0;
        var processedCount = distilledCount + discardedCount;

        if (allIslands.Count == 0)
            issues.Add("No islands were captured.");
        else if (processedCount < allIslands.Count)
            issues.Add($"{allIslands.Count - processedCount} island(s) still unprocessed.");

        if (!(brain?.Groups.Any() ?? false))
            issues.Add("No groups were formed.");

        if (!context.Decisions.Any())
            issues.Add("No decisions were recorded.");

        if (!context.Deliverables.Any())
            issues.Add("No deliverables were recorded.");

        var summary = issues.Count == 0
            ? $"All checks passed — " +
              $"islands: {allIslands.Count} ({distilledCount} distilled, {discardedCount} discarded), " +
              $"groups: {brain?.Groups.Count ?? 0}, " +
              $"decisions: {context.Decisions.Count}, " +
              $"deliverables: {context.Deliverables.Count}."
            : $"{issues.Count} issue(s): {string.Join(" | ", issues)}";

        return new AgentValidation(issues.Count == 0, summary, issues.AsReadOnly());
    }
}

/// <summary>
/// Result produced by the ClosedStep.
/// ApplyTo seals the checkpoint with the ClosedAt timestamp.
/// </summary>
public record ClosedResult(
    string          Output,
    bool            GateSatisfied,
    AgentValidation Validation,
    int             TotalInputTokens,
    int             TotalOutputTokens,
    DateTime        ClosedAt) : StepResult(Output, GateSatisfied)
{
    public int TotalTokens => TotalInputTokens + TotalOutputTokens;

    public override void ApplyTo(ISessionWriter writer)
    {
        writer.FinalizeSession(ClosedAt);
    }
}
