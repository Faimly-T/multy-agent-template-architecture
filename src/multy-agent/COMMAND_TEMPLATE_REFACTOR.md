# Command Template Refactor — Implementation Plan

## Context

This plan introduces a **Template Method pattern for prompt assembly** at the command layer, replacing the hidden `ResolveAndBuildAsync` helper in `CommandHandlerBase` with an explicit, overridable `BuildPrompt` method. It also removes `ICommandPromptLayer` as a distinct interface, simplifies `PromptContext` to a pure data carrier, and makes the three-layer ownership contract visible and enforced by constructor signatures.

---

## Architecture in One Page

### The Three-Layer Ownership Contract

Each architectural layer owns one dimension of the final LLM message. Constructor signatures enforce this:

| Layer | Class | Owns | Receives at construction |
|---|---|---|---|
| **Aggregate** | `AgentAggregate<TId>` | Role | `Role` (from config) |
| **Step** | `AgentStep` | Step context carrier | `IStepPromptLayer` (from factory, Role already set) |
| **Command** | `CommandHandlerBase` | Synthesis instruction | `IStepPromptLayer` (from step) + `string synthesisInstruction` |

### What Changes

**`ICommandPromptLayer` is deleted.** The assembly responsibility it held (`BuildMessages`) moves into `CommandHandlerBase.BuildPrompt` — a `virtual` method that each command can override. `PromptContext` no longer builds `ChatMessage[]`; it only carries data.

**`IStepPromptLayer` loses terminal methods.** `ForCommand`, `RoleOnly`, and `WithoutSkills` are removed. The interface now exposes `RolePrompt` and `Skills` as readable properties so `BuildPrompt` can read them directly.

**`CommandHandlerBase` loses its helper layer.** `ResolveAndBuildAsync`, `ResolveSkillsAsync`, `SelectViaLlmAsync`, `abstract SynthesisInstruction`, `SkillNames`, and `AutoSelectSkills` are all removed. Each command's `ExecuteAiCommandAsync` owns skill strategy explicitly. `BuildPrompt` is the clean handoff into the context.

**`IPipelineFactory` receives `IStepPromptLayer` instead of `Role`.** The aggregate builds context eagerly and passes it to the factory, which passes it to each step constructor.

### New Context Flow

```
AgentAggregate constructor
  → _agentContext = PromptContext.Empty.WithRole(Role)   // IStepPromptLayer, role set

AgentAggregate.RunAsync
  → Pipeline = await pipelineFactory.CreatePipelineAsync(_agentContext, ct)

IPipelineFactory.CreatePipelineAsync(IStepPromptLayer agentContext, ct)
  → new KickoffStep(agentContext, ...)   // step stores _stepContext
  → new CaptureStep(agentContext, ...)
  → ...

AgentStep.ExecuteStepAsync
  → new DefaultStepCommand(_stepContext, Instructions, this)

CommandHandlerBase constructor(IStepPromptLayer stepCtx, string synthesisInstruction)
  → stores _stepContext and SynthesisInstruction

ExecuteAiCommandAsync (each concrete command)
  → decides skill strategy (none / Pattern A explicit / Pattern B LLM-driven)
  → resolves skills if needed via injected ISkillResolver
  → var messages = BuildPrompt(userContent, skills)   // default template or override

CommandHandlerBase.BuildPrompt (virtual — default template)
  → reads _stepContext.RolePrompt + SynthesisInstruction
  → assembles [System: role + skills + instruction, User: userContent]
  → returns IReadOnlyList<ChatMessage>    // the only place ChatMessage[] are built
```

### Default BuildPrompt Template

```csharp
protected virtual IReadOnlyList<ChatMessage> BuildPrompt(
    string userContent, IReadOnlyList<Skill> skills = [])
{
    var parts = new List<string>();

    if (!string.IsNullOrWhiteSpace(_stepContext.RolePrompt))
        parts.Add($"You will act with the following Role:\n{_stepContext.RolePrompt}");

    if (skills.Count > 0)
    {
        var skillsBlock = string.Join("\n\n",
            skills.Select(s => $"### Skill: {s.Name}\n{s.Instructions}"));
        parts.Add($"Available skill frameworks to execute the instruction:\n{skillsBlock}");
    }

    parts.Add($"You will execute the following instruction:\n{SynthesisInstruction}");

    return [
        new ChatMessage(MessageRole.System, string.Join("\n\n", parts)),
        new ChatMessage(MessageRole.User, userContent)
    ];
}
```

---

## Files Changed Summary

| File | Action |
|---|---|
| `Agent/Prompts/IStepPromptLayer.cs` | **MODIFY** — add `RolePrompt`, `Skills`; remove `ForCommand`, `RoleOnly`, `WithoutSkills` |
| `Agent/Prompts/ICommandPromptLayer.cs` | **DELETE** |
| `Agent/Prompts/PromptContext.cs` | **MODIFY** — remove `ICommandPromptLayer`, `BuildMessages`, `ForCommand`, `RoleOnly`, `WithoutSkills`; expose `RolePrompt` and `Skills` |
| `Agent/Ports/IPipelineFactory.cs` | **MODIFY** — `CreatePipelineAsync(Role)` → `CreatePipelineAsync(IStepPromptLayer)` |
| `Agent/Steps/AgentStep.cs` | **MODIFY** — add `IStepPromptLayer _stepContext` field + constructor param; remove `BuildStepContext` |
| `Agent/Steps/CODESteps/KickoffChain/Handlers/CommandHandlerBase.cs` | **MODIFY** — full redesign: constructor, `virtual BuildPrompt`, remove all helpers |
| `Agent/Steps/CODESteps/DefaultStepCommand.cs` | **MODIFY** — update constructor; replace `ResolveAndBuildAsync` with `BuildPrompt` |
| `Agent/IAgentRunContext.cs` | **MODIFY** — remove `PromptContext` property (now unused) |
| `Agent/AgentAggregate.cs` | **MODIFY** — build `_agentContext` eagerly; pass `IStepPromptLayer` to factory |
| `Agent/Steps/CODESteps/KickoffChain/Handlers/CheckpointValidatorHandler.cs` | **MODIFY** — update constructor; replace `ResolveAndBuildAsync` with `BuildPrompt` |
| `Agent/Steps/CODESteps/KickoffChain/Handlers/QuestionTriageHandler.cs` | **MODIFY** — update constructor; replace `ResolveAndBuildAsync` with `BuildPrompt` |
| `Agent/Steps/CODESteps/KickoffChain/Handlers/ObjectiveSynthesisHandler.cs` | **MODIFY** — update constructor; move skill resolution into `ExecuteAiCommandAsync`; use `BuildPrompt` |
| `Agent/Steps/CODESteps/KickoffStep.cs` | **MODIFY** — update constructor to receive `IStepPromptLayer`; pass `_stepContext` to handlers |

---

## Step 1 — Simplify `IStepPromptLayer`

**File:** `AgentFramework.Core/Agent/Prompts/IStepPromptLayer.cs`

Remove the terminal methods (`ForCommand`, `RoleOnly`, `WithoutSkills`) — those existed to produce `ICommandPromptLayer` which is being deleted. Add `RolePrompt` and `Skills` as readable properties so `CommandHandlerBase.BuildPrompt` can read them directly without going through a terminal interface.

**Replace entire file with:**

```csharp
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Prompts;

public interface IStepPromptLayer
{
    string?              RolePrompt { get; }
    IReadOnlyList<Skill> Skills     { get; }
    IStepPromptLayer     WithSkills(IReadOnlyList<Skill> skills);
}
```

---

## Step 2 — Delete `ICommandPromptLayer`

**File:** `AgentFramework.Core/Agent/Prompts/ICommandPromptLayer.cs`

Delete the file entirely. The assembly responsibility it held (`BuildMessages`) now lives in `CommandHandlerBase.BuildPrompt`. Nothing should reference `ICommandPromptLayer` after Step 1.

---

## Step 3 — Simplify `PromptContext`

**File:** `AgentFramework.Core/Agent/Prompts/PromptContext.cs`

Remove `ICommandPromptLayer` from the implements list. Remove `BuildMessages`, `ForCommand`, `RoleOnly`, `WithoutSkills`. Make `RolePrompt` and `Skills` publicly readable (satisfying the new `IStepPromptLayer` contract). Keep `WithSkills` and `WithRole`.

**Replace entire file with:**

```csharp
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Prompts;

public sealed record PromptContext : IAgentPromptLayer, IStepPromptLayer
{
    public string?              RolePrompt { get; init; }
    public IReadOnlyList<Skill> Skills     { get; init; } = [];

    public static PromptContext Empty => new();

    IStepPromptLayer IAgentPromptLayer.WithRole(Role role)
        => this with { RolePrompt = role?.BuildRolePrompt };

    IStepPromptLayer IStepPromptLayer.WithSkills(IReadOnlyList<Skill> skills)
        => this with { Skills = skills };
}
```

Note: `RolePrompt` and `Skills` are now `public` — `BuildPrompt` in `CommandHandlerBase` reads them via the `IStepPromptLayer` interface.

---

## Step 4 — Update `IPipelineFactory`

**File:** `AgentFramework.Core/Agent/Ports/IPipelineFactory.cs`

Change the parameter from `Role` to `IStepPromptLayer`. The aggregate now builds the context with role and passes it to the factory directly — the factory no longer needs to know about `Role`.

**Replace entire file with:**

```csharp
using AgentFramework.Core.Agent.Prompts;

namespace AgentFramework.Core.Agent.Ports;

public interface IPipelineFactory
{
    Task<Steps.StepPipeline> CreatePipelineAsync(
        IStepPromptLayer agentContext,
        CancellationToken ct = default);
}
```

---

## Step 5 — Update `AgentStep`

**File:** `AgentFramework.Core/Agent/Steps/AgentStep.cs`

Add `IStepPromptLayer _stepContext` as a protected field. Add it as the first constructor parameter — the factory passes it when creating each step. Remove `BuildStepContext(IAgentRunContext?)` — it was the old bridge from `IAgentRunContext` to prompt context, now unnecessary. Update `ExecuteStepAsync` to use the stored `_stepContext`.

**Key changes:**

```csharp
// ADD field
protected readonly IStepPromptLayer _stepContext;

// CHANGE constructor signature — IStepPromptLayer is first param
protected AgentStep(
    IStepPromptLayer stepContext,
    int stepNumber,
    string skillName,
    string instructions,
    Gate gate)
{
    _stepContext = stepContext;
    StepNumber   = stepNumber;
    SkillName    = skillName;
    Instructions = instructions;
    Gate         = gate;
}

// REMOVE BuildStepContext method entirely

// UPDATE ExecuteStepAsync — use _stepContext instead of BuildStepContext(context)
public virtual async Task<StepResult?> ExecuteStepAsync(
    IAgentRunContext? context,
    ISessionWriter writer,
    IChatClient chatClient,
    CancellationToken ct = default)
{
    var command = new DefaultStepCommand(_stepContext, Instructions, this);
    await command.ExecuteAiCommandAsync([], context, writer, chatClient, ct);

    var result = command.LastResult!;
    writer.RecordStepExchange(StepNumber, Name, command.LastMessages, result.Output);
    return result;
}
```

---

## Step 6 — Redesign `CommandHandlerBase`

**File:** `AgentFramework.Core/Agent/Steps/CODESteps/KickoffChain/Handlers/CommandHandlerBase.cs`

Full redesign. Constructor now takes `(IStepPromptLayer stepCtx, string synthesisInstruction, ISkillResolver? resolver = null)`. Add `virtual BuildPrompt`. Keep `SelectViaLlmAsync` as a `protected` helper — commands that use Pattern B call it directly in their `ExecuteAiCommandAsync`. Remove `ResolveAndBuildAsync`, `ResolveSkillsAsync`, `abstract SynthesisInstruction`, `SkillNames`, `AutoSelectSkills`.

**Replace entire file with:**

```csharp
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain.Handlers;

internal abstract class CommandHandlerBase(
    IStepPromptLayer? stepCtx            = null,
    string            synthesisInstruction = "",
    ISkillResolver?   resolver            = null) : ICommandHandler
{
    private readonly IStepPromptLayer _stepContext = stepCtx ?? PromptContext.Empty;
    protected readonly string         SynthesisInstruction = synthesisInstruction;
    protected readonly ISkillResolver? Resolver             = resolver;

    // Default template: Role + Skills + SynthesisInstruction → [System, User]
    // Override in commands that need a different composition.
    protected virtual IReadOnlyList<ChatMessage> BuildPrompt(
        string userContent, IReadOnlyList<Skill> skills = [])
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(_stepContext.RolePrompt))
            parts.Add($"You will act with the following Role:\n{_stepContext.RolePrompt}");

        if (skills.Count > 0)
        {
            var skillsBlock = string.Join("\n\n",
                skills.Select(s => $"### Skill: {s.Name}\n{s.Instructions}"));
            parts.Add($"Available skill frameworks to execute the instruction:\n{skillsBlock}");
        }

        parts.Add($"You will execute the following instruction:\n{SynthesisInstruction}");

        return [
            new ChatMessage(MessageRole.System, string.Join("\n\n", parts)),
            new ChatMessage(MessageRole.User, userContent)
        ];
    }

    // Shared helper for Pattern B — commands call this directly inside ExecuteAiCommandAsync
    // when they want the LLM to select skills from the catalog.
    protected async Task<IReadOnlyList<string>> SelectViaLlmAsync(
        IReadOnlyList<SkillSummary> catalog, IChatClient chatClient, CancellationToken ct)
    {
        if (catalog.Count == 0) return [];

        var schema       = """{ "selectedSkills": ["skill-name-1"] }""";
        var catalogLines = string.Join("\n", catalog.Select(s => $"- {s.Name}: {s.Description}"));
        var userContent  = $"""
            Available skills:
            {catalogLines}

            Select the skills most relevant to:
            {SynthesisInstruction}

            Respond with JSON listing the selected skill names.
            """;

        // Use role-only prompt for selection — no skills needed to select skills
        var messages = BuildPrompt(userContent);

        return await chatClient.SendHandlerAsync(
            messages,
            schema,
            root => root.TryGetProperty("selectedSkills", out var arr)
                ? (IReadOnlyList<string>)arr.EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .Where(s => s.Length > 0)
                    .ToList()
                : [],
            ct);
    }

    public abstract Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext?  contextAgent,
        ISessionWriter     writer,
        IChatClient?       chatClient,
        CancellationToken  ct);
}
```

---

## Step 7 — Update `DefaultStepCommand`

**File:** `AgentFramework.Core/Agent/Steps/CODESteps/DefaultStepCommand.cs`

Update constructor to match the new `CommandHandlerBase` signature — pass `synthesisInstruction` explicitly instead of having it as an abstract property override. Replace `ResolveAndBuildAsync` call with `BuildPrompt`. No skills for the default command.

**Replace entire file with:**

```csharp
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain;
using AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain.Handlers;

namespace AgentFramework.Core.Agent.Steps.CODESteps;

internal sealed class DefaultStepCommand : CommandHandlerBase
{
    private readonly AgentStep _step;

    public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];
    public StepResult? LastResult { get; private set; }

    public DefaultStepCommand(IStepPromptLayer stepCtx, string instructions, AgentStep step)
        : base(stepCtx, instructions)
    {
        _step = step;
    }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext? contextAgent,
        ISessionWriter writer,
        IChatClient? chatClient,
        CancellationToken ct)
    {
        var userContext = _step.BuildContext(contextAgent);
        var schema = $"""
            ### Required Response Format
            Respond ONLY with a JSON object matching this schema:
            ```json
            {_step.JsonResponseSchema}
            ```
            Gate: {_step.Gate.Description}
            """;

        var fullUser = string.IsNullOrWhiteSpace(userContext)
            ? schema
            : $"{userContext}\n\n{schema}";

        // Default command: no skills — role + synthesisInstruction only
        var messages = BuildPrompt(fullUser);
        LastMessages = messages;

        var result = await chatClient!.SendAsync(messages, _step, ct);
        LastResult = result;

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(context.LastOutput() ?? "[start]", result.Output),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }
}
```

---

## Step 8 — Update `IAgentRunContext`

**File:** `AgentFramework.Core/Agent/IAgentRunContext.cs`

Remove the `PromptContext` property. It was used only by `AgentStep.BuildStepContext` (deleted in Step 5). Steps now receive their context at construction via the factory — `IAgentRunContext` reverts to session-data only.

**Remove this line:**

```csharp
IStepPromptLayer PromptContext { get; }
```

---

## Step 9 — Update `AgentAggregate`

**File:** `AgentFramework.Core/Agent/AgentAggregate.cs`

Remove the lazy `_basePromptContext` field. Build the agent context eagerly in the constructor (or at the start of `RunAsync` before the factory call — since `RunAsync` is the execution entry point, building there is the right moment). Pass `IStepPromptLayer` to the factory instead of `Role`. Remove the `IAgentRunContext.PromptContext` explicit implementation.

**Key changes:**

```csharp
// REMOVE this field:
private IStepPromptLayer? _basePromptContext;

// UPDATE RunAsync — build context here, pass to factory:
public async Task<AgentRunResult> RunAsync(
    string userIntent,
    IChatClient chatClient,
    IDeliverableWriter deliverableWriter,
    IPipelineFactory pipelineFactory,
    CancellationToken ct = default)
{
    // Build agent context once — Role is invariant for the lifetime of a run
    var agentContext = ((IAgentPromptLayer)PromptContext.Empty).WithRole(Role);

    Pipeline = await pipelineFactory.CreatePipelineAsync(agentContext, ct);
    Session?.SetUserIntent(userIntent);

    // ... rest of RunAsync unchanged
}

// REMOVE this explicit implementation:
IStepPromptLayer IAgentRunContext.PromptContext
    => _basePromptContext ??= ((IAgentPromptLayer)PromptContext.Empty).WithRole(Role);
```

---

## Step 10 — Update `CheckpointValidatorCommand`

**File:** `AgentFramework.Core/Agent/Steps/CODESteps/KickoffChain/Handlers/CheckpointValidatorHandler.cs`

Update constructor to pass the synthesis instruction string to base explicitly. Replace `ResolveAndBuildAsync` with `BuildPrompt`. No skills for this handler — checkpoint validation is role-only.

**Replace entire file with:**

```csharp
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain.Handlers;

internal sealed class CheckpointValidatorCommand : CommandHandlerBase
{
    private const string Schema = """
        {
          "sessionContext": "Summary context of previous sessions goals to be used in the definition of the next goal based on the user request"
        }
        """;

    private const string Instruction =
        "You are an assistant helping to Kickoff the context of an agent's previous session, " +
        "to define the Goal of the next iteration. " +
        "Review the following list of previous checkpoints and generate a summary context of the " +
        "previous sessions goals as valid information to be used in the definition of the next goal " +
        "based on the user request.";

    public CheckpointValidatorCommand(IStepPromptLayer? stepCtx = null)
        : base(stepCtx, Instruction) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext? contextAgent,
        ISessionWriter writer,
        IChatClient? chatClient,
        CancellationToken ct)
    {
        var lines = new List<string>();

        foreach (var checkpoint in
            contextAgent?.Session?.Checkpoints.OrderByDescending(c => c.SessionIteration)
            ?? Enumerable.Empty<Checkpoint>())
        {
            lines.Add($"Checkpoint #{checkpoint.SessionIteration} " +
                      $"({checkpoint.CreatedAt:yyyy-MM-dd}): " +
                      $"Focused on {checkpoint.SessionObjective}. " +
                      $"Accomplished: {string.Join(", ", checkpoint.Accomplishments?.Take(3) ?? Enumerable.Empty<string>())}");
        }

        // TODO: if no checkpoints, skip the LLM call and return empty context to avoid hallucination
        if (lines.Count == 0)
        {
            return new HandlerExchange(
                GetType().Name,
                new HandlerContent(context.LastOutput() ?? "[start]", "No previous checkpoints available."),
                new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
        }

        var userContent = $"""
            Previous checkpoints:
            {string.Join("\n", lines)}
            """;

        // No skills — role + instruction only
        var messages = BuildPrompt(userContent);
        var json = await chatClient!.SendHandlerAsync(messages, Schema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(context.LastOutput() ?? "[start]", json),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }
}
```

---

## Step 11 — Update `QuestionTriageHandler`

**File:** `AgentFramework.Core/Agent/Steps/CODESteps/KickoffChain/Handlers/QuestionTriageHandler.cs`

Update constructor to pass the instruction string to base. Replace `ResolveAndBuildAsync` with `BuildPrompt`. No skills for this handler.

**Constructor and ExecuteAiCommandAsync changes:**

```csharp
private const string Instruction =
    "You are triaging open questions from a prior agent session. " +
    "Based on the iteration context provided, classify each question. " +
    "Respond with valid JSON only.";

// CHANGE constructor:
internal sealed class QuestionTriageHandler(
    IMarkFileReader   reader,
    IStepPromptLayer? stepCtx = null)
    : CommandHandlerBase(stepCtx, Instruction)

// CHANGE in ExecuteAiCommandAsync — replace:
//   var messages = await ResolveAndBuildAsync(userContent, chatClient, ct);
// with:
var messages = BuildPrompt(userContent);   // no skills — role + instruction only
```

All other logic in `ExecuteAiCommandAsync` remains unchanged.

---

## Step 12 — Update `ObjectiveSynthesisHandler`

**File:** `AgentFramework.Core/Agent/Steps/CODESteps/KickoffChain/Handlers/ObjectiveSynthesisHandler.cs`

Update constructor. Move skill resolution explicitly into `ExecuteAiCommandAsync` — the command now owns that strategy decision. Use `BuildPrompt(userContent, skills)` with resolved skills, or `BuildPrompt(userContent)` if no resolver is configured.

**Replace entire file with:**

```csharp
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain.Handlers;

internal sealed class ObjectiveSynthesisHandler : CommandHandlerBase
{
    private const string Schema = """
        {
          "sessionObjective": "verb + deliverable + success condition + stakes clause",
          "narrativeBridge": "2-3 sentences connecting prior session to this one",
          "isInitialSession": false,
          "stalenessWarning": "string or null",
          "blockers": [{ "questionId": "Q-001", "text": "...", "severity": "hard|soft" }],
          "gateSatisfied": true
        }
        """;

    private const string Instruction =
        "Synthesize a precise session objective from the context below. " +
        "Respond with valid JSON only.";

    public ObjectiveSynthesisHandler(
        IStepPromptLayer? stepCtx  = null,
        ISkillResolver?   resolver = null)
        : base(stepCtx, Instruction, resolver) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext? contextAgent,
        ISessionWriter writer,
        IChatClient? chatClient,
        CancellationToken ct)
    {
        var checkpointContext = context.GetOutput(nameof(CheckpointValidatorCommand));
        var markFileContext   = context.GetOutput(nameof(MarkFileLoaderHandler));
        var questionContext   = context.GetOutput(nameof(QuestionTriageHandler));

        var userInput = contextAgent?.Session?.UserIntent;
        var userInputLine = string.IsNullOrWhiteSpace(userInput)
            ? string.Empty
            : $"\nUser stated intent: {userInput}\n";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(checkpointContext)) parts.Add(checkpointContext);
        if (!string.IsNullOrWhiteSpace(markFileContext))   parts.Add(markFileContext);
        if (!string.IsNullOrWhiteSpace(questionContext))   parts.Add(questionContext);

        var userContent = $"""
            {string.Join("\n\n", parts)}{userInputLine}

            Synthesize:
              1. sessionObjective — verb + deliverable + success condition + stakes clause
              2. narrativeBridge — 2-3 sentences connecting prior session end to this session start
              3. isInitialSession — true only if no prior session exists
              4. stalenessWarning — include if checkpoint is stale, otherwise null
              5. blockers — list of still-open blockers from prior context, with questionId, text, and severity
              6. gateSatisfied — true if objective is well-formed, false if missing required elements
            """;

        // Skill strategy: Pattern A — explicit names if resolver is configured.
        // Command owns this decision. Override BuildPrompt for a different composition.
        IReadOnlyList<Skill> skills = [];
        if (Resolver is not null)
            skills = await Resolver.ResolveAsync(["kickoff-context"], ct);

        var messages = BuildPrompt(userContent, skills);
        var json = await chatClient!.SendHandlerAsync(messages, Schema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(context.LastOutput() ?? "[start]", json),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }
}
```

---

## Step 13 — Update `KickoffStep`

**File:** `AgentFramework.Core/Agent/Steps/CODESteps/KickoffStep.cs`

Add `IStepPromptLayer stepContext` as the first constructor parameter (satisfying the new `AgentStep` base constructor). Use stored `_stepContext` when building chain handlers instead of calling `BuildStepContext(context)`. Update `ExecuteStepAsync` — `stepCtx.RoleOnly.BuildMessages(...)` for tracking messages is replaced with `_stepContext`-based construction using the new `BuildPrompt`.

**Key constructor change:**

```csharp
public KickoffStep(
    IStepPromptLayer   stepContext,
    int                stepNumber,
    string             instructions,
    Gate               gate,
    IMarkFileReader?   markFileReader = null,
    ISkillResolver?    skillResolver  = null)
    : base(stepContext, stepNumber, "Kickoff-context", instructions, gate)
{
    _markFileReader = markFileReader ?? new NullMarkFileReader();
    _skillResolver  = skillResolver;
}
```

**Key `ExecuteStepAsync` change** — replace `BuildStepContext(context)` with `_stepContext`:

```csharp
public override async Task<StepResult?> ExecuteStepAsync(
    IAgentRunContext? context,
    ISessionWriter writer,
    IChatClient chatClient,
    CancellationToken ct = default)
{
    // Use _stepContext stored at construction — no IAgentRunContext needed for prompt
    var chain = new KickoffContextChain(
        new CheckpointValidatorCommand(_stepContext),
        new MarkFileLoaderHandler(_markFileReader),
        new IterationEvaluatorHandler(),
        new QuestionTriageHandler(_markFileReader, _stepContext),
        new ObjectiveSynthesisHandler(_stepContext, _skillResolver));

    var (finalJson, journal) = await chain.RunAsync(context, writer, chatClient, ct);

    Journal.Clear();
    Journal.AddRange(journal);

    // Tracking messages for session history — role only, no skills needed for audit trail
    var trackingMessages = BuildPrompt($"## Step {StepNumber}: {Name}");
    writer.RecordStepExchange(StepNumber, Name, trackingMessages, finalJson);

    using var doc = JsonDocument.Parse(finalJson);
    var gateSatisfied = !doc.RootElement.TryGetProperty("gateSatisfied", out var gs) || gs.GetBoolean();
    return ParseResult(doc.RootElement, finalJson, gateSatisfied);
}
```

Note: `BuildPrompt` here is available because `KickoffStep` inherits `CommandHandlerBase` indirectly through... wait — actually `KickoffStep` extends `AgentStep`, not `CommandHandlerBase`. `BuildPrompt` is not available directly on the step. For the tracking messages line, use `_stepContext` directly:

```csharp
// Tracking messages — construct manually using stored context
var trackingMessages = new List<ChatMessage>();
if (!string.IsNullOrWhiteSpace(_stepContext.RolePrompt))
    trackingMessages.Add(new ChatMessage(MessageRole.System, _stepContext.RolePrompt));
trackingMessages.Add(new ChatMessage(MessageRole.User, $"## Step {StepNumber}: {Name}"));
writer.RecordStepExchange(StepNumber, Name, trackingMessages, finalJson);
```

---

## Step 14 — Compile Verification

Run these in order. The project should build cleanly after Step 3 breaks are resolved by Step 6.

```bash
cd src/multy-agent/src
dotnet build AgentFramework.Core/AgentFramework.Core.csproj
dotnet build AgentFramework.Domain/AgentFramework.Domain.csproj
dotnet build AgentFramework.Infrastructure/AgentFramework.Infrastructure.csproj
```

### Expected errors and fixes

| Error | Cause | Fix |
|---|---|---|
| `ICommandPromptLayer` not found | Step 2 deleted it | Confirm all `using` statements referencing it are removed (Steps 3, 6) |
| `ForCommand` / `RoleOnly` / `WithoutSkills` not found on `IStepPromptLayer` | Step 1 removed them | Step 3 removes them from `PromptContext`; Steps 6–13 replace all call sites |
| `IPipelineFactory.CreatePipelineAsync` wrong arg type | Step 4 changed signature to `IStepPromptLayer` | Step 9 updates the call in `AgentAggregate.RunAsync`; domain factory implementations must update too |
| `AgentStep` constructor arg count mismatch | Step 5 added `IStepPromptLayer` as first param | Step 13 updates `KickoffStep`; update `CaptureStep`, `OrganizeStep`, `DistillStep`, `ExpressStep` the same way |
| `ResolveAndBuildAsync` not found | Step 6 removed it | Steps 10–12 replace with `BuildPrompt` |
| `abstract SynthesisInstruction` not found | Step 6 removed the abstract property | Steps 10–12 convert to `const string` fields passed to base constructor |
| `IAgentRunContext.PromptContext` not found | Step 8 removed it | Step 9 removes the `AgentAggregate` implementation; confirm no other call site references it |
| `BuildStepContext` not found | Step 5 removed it | Step 13 replaces usage in `KickoffStep` |

### Additional step subclasses to update

`CaptureStep`, `OrganizeStep`, `DistillStep`, and `ExpressStep` all extend `AgentStep`. Each needs `IStepPromptLayer stepContext` added as the first constructor parameter, forwarded to `base(stepContext, ...)`. No other changes needed in these files.

---

## Invariants After Refactor

- `PromptContext` is a pure data carrier. It never builds `ChatMessage[]`.
- `ChatMessage[]` are built only in `CommandHandlerBase.BuildPrompt` (or overrides).
- `SynthesisInstruction` is always a concrete `string` passed to the base constructor — never an abstract property override.
- Skill resolution is always explicit in `ExecuteAiCommandAsync` — no hidden framework logic.
- `IAgentRunContext` carries session data only — no prompt context concern.
- `IPipelineFactory` receives a ready `IStepPromptLayer` — it never builds context itself.
