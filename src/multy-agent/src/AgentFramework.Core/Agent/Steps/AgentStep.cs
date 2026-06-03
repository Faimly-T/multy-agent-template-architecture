using System.Text.Json;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps.CODESteps;

namespace AgentFramework.Core.Agent.Steps;

/// <summary>
/// Abstract base class for all pipeline steps. Implements the <b>Template Method</b> pattern.
///
/// The fixed algorithm in <see cref="ExecuteStepAsync"/> handles two paths:
/// <list type="number">
///   <item>
///     <b>Chain path</b> (preferred): runs a <see cref="IStepChain"/> of domain handlers,
///     records the full journal, then calls the virtual hooks below so subclasses control
///     gate evaluation and result shape without duplicating orchestration code.
///   </item>
///   <item>
///     <b>Single-LLM fallback</b>: when no chain is configured, a single
///     <c>DefaultStepCommand</c> call wraps the step's schema and instructions.
///     Useful for simple steps or rapid prototyping.
///   </item>
/// </list>
///
/// <b>Extension points (override in concrete steps):</b>
/// <list type="bullet">
///   <item><see cref="EvaluateGate"/> — decide whether the gate is satisfied from parsed JSON or journal data.</item>
///   <item><see cref="BuildChainResult"/> — build a richer result type carrying artifacts from the journal.</item>
///   <item><see cref="ParseResult"/> — deserialise the final JSON into a typed <see cref="StepResult"/> subclass.</item>
///   <item><see cref="BuildContext"/> — inject session state into the user prompt.</item>
/// </list>
/// </summary>
public abstract class AgentStep
{
    public string Name         => GetType().Name;
    public int    StepNumber   { get; }
    public string Instructions { get; }
    public Gate   Gate         { get; }

    // Context budget management is the responsibility of each handler's prompt construction.
    // Following the Denis Rothman model: each handler compresses prior context before calling
    // the LLM, keeping every individual call within the model's context window.

    public abstract string JsonResponseSchema { get; }

    protected readonly IStepPromptLayer _stepContext;

    /// <summary>
    /// Names of the skills resolved by <c>AgentBuilder</c> and loaded into this step's prompt
    /// context. Reads directly from <see cref="_stepContext"/>.Skills — the authoritative source.
    /// </summary>
    public IReadOnlyList<string> SkillNames
        => _stepContext.Skills.Select(s => s.Name).ToList().AsReadOnly();

    /// <summary>
    /// The handler chain to run for this step. When <c>null</c> the step falls back
    /// to a single LLM call via the base <see cref="ExecuteStepAsync"/> path.
    /// </summary>
    protected IStepChain? Chain { get; }

    private readonly Func<string, string?>? _instructionLookup;

    protected AgentStep(
        IStepPromptLayer       stepContext,
        int                    stepNumber,
        string                 instructions,
        Gate                   gate,
        IStepChain?            chain              = null,
        Func<string, string?>? instructionLookup  = null)
    {
        _stepContext        = stepContext;
        StepNumber          = stepNumber;
        Instructions        = instructions;
        Gate                = gate;
        Chain               = chain;
        _instructionLookup  = instructionLookup;
    }

    public virtual string BuildContext(IAgentRunContext? context) => string.Empty;

    public abstract StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied);

    // ── Template method hooks (override in subclasses) ───────────────────────

    /// <summary>
    /// Determines whether the step's gate is satisfied after a chain run.
    /// Default: reads the <c>gateSatisfied</c> boolean from the final JSON output.
    /// Override to base the decision on parsed domain data (e.g. island count).
    /// </summary>
    protected virtual bool EvaluateGate(JsonElement root, IReadOnlyList<HandlerExchange> journal)
        => !root.TryGetProperty("gateSatisfied", out var gs) || gs.GetBoolean();

    /// <summary>
    /// Builds the <see cref="StepResult"/> after a chain run.
    /// Default: calls <see cref="ParseResult"/> with the evaluated gate flag.
    /// Override to produce a richer result type that carries artifacts from the journal.
    /// </summary>
    protected virtual StepResult BuildChainResult(
        JsonElement root, string rawOutput, bool gateSatisfied,
        IReadOnlyList<HandlerExchange> journal)
        => ParseResult(root, rawOutput, gateSatisfied);

    // ── Execution ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Executes the step. When a <see cref="Chain"/> is present, runs all handlers
    /// sequentially, records the journal, evaluates the gate, and builds the result via
    /// the virtual hooks above. When no chain is present, falls back to a single LLM call.
    /// </summary>
    public virtual async Task<StepResult?> ExecuteStepAsync(
        IAgentRunContext? context,
        ISessionWriter    writer,
        IChatClient       chatClient,
        CancellationToken ct = default)
    {
        if (Chain is null)
        {
            // Single LLM call path
            var command  = new DefaultStepCommand(_stepContext, Instructions, this);
            var exchange = await command.ExecuteAiCommandAsync([], context, writer, chatClient, ct);
            writer.RecordStepJournal(StepNumber, Name, [exchange]);
            return command.LastResult!;
        }

        // Chain path — template method
        var (finalJson, journal) = await Chain.RunAsync(context, writer, chatClient, _instructionLookup, ct);
        writer.RecordStepJournal(StepNumber, Name, journal);

        using var doc     = JsonDocument.Parse(finalJson);
        var gateSatisfied = EvaluateGate(doc.RootElement, journal);
        return BuildChainResult(doc.RootElement, finalJson, gateSatisfied, journal);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Reads <c>gateSatisfied</c> from JSON, defaulting to <c>true</c> when absent.</summary>
    protected static bool ReadGateSatisfied(JsonElement root)
        => !root.TryGetProperty("gateSatisfied", out var gs) || gs.GetBoolean();

    /// <summary>Parses the final chain JSON using its own <c>gateSatisfied</c> field.</summary>
    protected StepResult ParseFinalJson(string finalJson)
    {
        using var doc = JsonDocument.Parse(finalJson);
        return ParseResult(doc.RootElement, finalJson, ReadGateSatisfied(doc.RootElement));
    }
}
