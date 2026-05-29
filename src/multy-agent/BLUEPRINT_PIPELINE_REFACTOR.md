# BLUEPRINT: Pipeline & Role Refactor

## Overview

This plan supersedes `COMMAND_TEMPLATE_REFACTOR.md`.

It covers three design goals that must be implemented together:

1. **`RoleDefinition` record** — move role concept fully into the Prompts layer as a serialisable record with a `ToPromptString()` method; remove the `Role` class.
2. **`PipelineCode` class** — a DI-injectable pipeline builder that resolves skills per-step and constructs the 5 typed `AgentStep` instances; `AgentAggregate` becomes pipeline-agnostic.
3. **`UxPersonaConfig` + `UxPersona.BuildAsync`** — serialisable config record (future CosmosDB document); static async factory replaces the public constructor.

---

## Current State (before)

| Thing | Current |
|---|---|
| `Role` | class in `AgentFramework.Core/Agent/Role.cs` with `BuildRolePrompt` computed property |
| `Identity` | class in same file |
| `RoleParser` | returns `Role` |
| `IAgentPromptLayer.WithRole` | takes `Role` |
| `Skill.Instructions` | third property of `Skill` record |
| `SkillParser` | builds `new Skill(name, description, instructions)` |
| `CommandHandlerBase.BuildPrompt` | uses `s.Instructions`; does not fall back to `_stepContext.Skills` |
| `ObjectiveSynthesisHandler` | explicitly calls `Resolver.ResolveAsync(["kickoff-context"])` |
| `KickoffStep` | takes `ISkillResolver?` and passes it to `ObjectiveSynthesisHandler` |
| `AgentAggregate` | has `Role Role` property; constructor takes `Role`; `RunAsync` takes `IPipelineFactory` |
| `IPipelineFactory` | exists; `UxPersonaTests` uses it via `TestPipelineFactory` |
| `UxPersona` | public constructor `(Role, AgentStep[], projectId, markFilePaths)` |
| `UxStepBuilder` | thin builder wrapping `StepPipeline` |
| `PipelineCode` | does not exist |
| `UxPersonaConfig` | does not exist |

## Target State (after)

| Thing | Target |
|---|---|
| `RoleDefinition` | immutable record in `Agent/Prompts/` with `ToPromptString()` |
| `Identity` | immutable record in same file |
| `RoleParser` | returns `RoleDefinition` |
| `IAgentPromptLayer.WithRole` | takes `RoleDefinition`; interface also exposes `RoleDefinition? Role` |
| `IStepPromptLayer` | adds `RoleDefinition? Role` property |
| `PromptContext` | stores `RoleDefinition? Role`; `RolePrompt` computed via `Role?.ToPromptString()` |
| `Skill.Content` | renamed from `Instructions` |
| `SkillParser` | builds `new Skill(name, description, content)` |
| `CommandHandlerBase.BuildPrompt` | uses `s.Content`; falls back to `_stepContext.Skills` when no skills passed |
| `ObjectiveSynthesisHandler` | no `ISkillResolver`; calls `BuildPrompt(userContent)` — skills from step context |
| `KickoffStep` | no `ISkillResolver`; skills come from `stepContext.Skills` |
| `AgentAggregate` | no `Role` property; constructor takes `IStepPromptLayer agentContext`; `RunAsync` has no `IPipelineFactory` |
| `IPipelineFactory` | **DELETED** |
| `UxPersona` | private constructor; `BuildAsync(UxPersonaConfig, PipelineCode, ct)` static factory |
| `UxStepBuilder` | **DELETED** (use `new StepPipeline(steps)` directly) |
| `PipelineCode` | new class; constructor-injected `ISkillResolver` + `IMarkFileReader?`; `BuildAsync` creates 5 typed steps |
| `UxPersonaConfig` | new serialisable record |

---

## Execution Order

Steps are ordered to keep the solution compilable at every checkpoint.
Steps 1–7 update the data model. Steps 8–12 update the command/handler layer.
Steps 13–16 update the aggregate and pipeline infrastructure.
Steps 17–20 update tests.

---

## Step 1 — Create `RoleDefinition` record

**Action:** Create new file.

**File:** `src/AgentFramework.Core/Agent/Prompts/RoleDefinition.cs`

```csharp
namespace AgentFramework.Core.Agent.Prompts;

public record Identity(string Role, string Persona, string Authority, string Boundary);

public record RoleDefinition(
    string Name,
    string Description,
    Identity Identity,
    string Mandate,
    IReadOnlyList<string> FactsAndDirectives)
{
    public string ToPromptString() => $"""
        You are {Identity.Persona}.
        Role: {Identity.Role}
        Authority: {Identity.Authority}
        Boundary: {Identity.Boundary}

        Mandate: {Mandate}

        Directives:
        {string.Join("\n", FactsAndDirectives.Select(d => $"- {d}"))}
        """;

    public string ToMd()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"name: {Name}");
        sb.AppendLine($"description: {Description}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Identity");
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|-------|-------|");
        sb.AppendLine($"| **Role** | {Identity.Role} |");
        sb.AppendLine($"| **Persona** | {Identity.Persona} |");
        sb.AppendLine($"| **Authority** | {Identity.Authority} |");
        sb.AppendLine($"| **Boundary** | {Identity.Boundary} |");
        sb.AppendLine();
        sb.AppendLine("## Mandate");
        sb.AppendLine($"> {Mandate}");
        sb.AppendLine();
        sb.AppendLine("## Facts & Directives");
        foreach (var directive in FactsAndDirectives)
            sb.AppendLine($"- {directive}");
        return sb.ToString();
    }
}
```

**Note:** `Identity` is defined here as a record; the old `Identity` class in `Role.cs` will be deleted in Step 7.

---

## Step 2 — Update `RoleParser` to return `RoleDefinition`

**File:** `src/AgentFramework.Core/Agent/RoleParser.cs`

Replace the return type and the construction call. Add `using AgentFramework.Core.Agent.Prompts;`.

```csharp
// BEFORE
return new Role(name, description, identity, mandate, directives);

// AFTER
return new RoleDefinition(name, description, identity, mandate, directives);
```

Also update `ParseIdentity` return type: `private static Identity ParseIdentity(...)` — the return type is now the record `Identity` from `Prompts`. The parsing logic is identical. Remove the local `Identity` class reference (it's now imported from the namespace).

Full method signature changes:
```csharp
// BEFORE
public static Role ParseFromMarkdown(string markdown)

// AFTER
public static RoleDefinition ParseFromMarkdown(string markdown)
```

---

## Step 3 — Update `IAgentPromptLayer`

**File:** `src/AgentFramework.Core/Agent/Prompts/IAgentPromptLayer.cs`

```csharp
namespace AgentFramework.Core.Agent.Prompts;

public interface IAgentPromptLayer
{
    static IAgentPromptLayer Empty => PromptContext.Empty;
    RoleDefinition? Role { get; }
    IStepPromptLayer WithRole(RoleDefinition role);
}
```

Adding `RoleDefinition? Role` here makes the role accessible at the aggregate layer without casting.

---

## Step 4 — Update `IStepPromptLayer`

**File:** `src/AgentFramework.Core/Agent/Prompts/IStepPromptLayer.cs`

```csharp
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Prompts;

public interface IStepPromptLayer
{
    RoleDefinition?      Role       { get; }
    string?              RolePrompt { get; }
    IReadOnlyList<Skill> Skills     { get; }
    IStepPromptLayer     WithSkills(IReadOnlyList<Skill> skills);
}
```

`RoleDefinition? Role` is added so any handler or step can access the full role object (e.g. for serialisation or logging), not just the rendered prompt string.

---

## Step 5 — Update `PromptContext`

**File:** `src/AgentFramework.Core/Agent/Prompts/PromptContext.cs`

The `RolePrompt` string is now derived — not stored. `Role` is the authoritative source.

```csharp
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Prompts;

public sealed record PromptContext : IAgentPromptLayer, IStepPromptLayer
{
    public RoleDefinition?      Role       { get; init; }
    public string?              RolePrompt => Role?.ToPromptString();
    public IReadOnlyList<Skill> Skills     { get; init; } = [];

    public static PromptContext Empty => new();

    RoleDefinition? IAgentPromptLayer.Role => Role;

    IStepPromptLayer IAgentPromptLayer.WithRole(RoleDefinition role)
        => this with { Role = role };

    IStepPromptLayer IStepPromptLayer.WithSkills(IReadOnlyList<Skill> skills)
        => this with { Skills = skills };
}
```

**Verification:** `PromptContext.Empty.WithRole(myRole)` returns a new `PromptContext` with `Role` set; `.RolePrompt` automatically delegates to `ToPromptString()`.

---

## Step 6 — Rename `Skill.Instructions` → `Skill.Content`

**File:** `src/AgentFramework.Core/Agent/Steps/Skill.cs`

```csharp
namespace AgentFramework.Core.Agent.Steps;

public record Skill(string Name, string Description, string Content);
```

This is a breaking rename. All call sites must be updated in subsequent steps.

---

## Step 7 — Update `SkillParser` for `Content`

**File:** `src/AgentFramework.Core/Agent/Steps/SkillParser.cs`

```csharp
// BEFORE
var instructions = ExtractBody(markdown);
return new Skill(name, description, instructions);

// AFTER
var content = ExtractBody(markdown);
return new Skill(name, description, content);
```

---

## Step 8 — Update `CommandHandlerBase.BuildPrompt`

**File:** `src/AgentFramework.Core/Agent/Steps/CODESteps/KickoffChain/Handlers/CommandHandlerBase.cs`

Two changes:
1. `s.Instructions` → `s.Content`
2. Default `skills` parameter to `_stepContext.Skills` when null — this decouples handlers from knowing skill names

```csharp
protected virtual IReadOnlyList<ChatMessage> BuildPrompt(
    string userContent, IReadOnlyList<Skill>? skills = null)
{
    var resolvedSkills = skills ?? _stepContext.Skills;
    var parts = new List<string>();

    if (!string.IsNullOrWhiteSpace(_stepContext.RolePrompt))
        parts.Add($"You will act with the following Role:\n{_stepContext.RolePrompt}");

    if (resolvedSkills.Count > 0)
    {
        var skillsBlock = string.Join("\n\n",
            resolvedSkills.Select(s => $"### Skill: {s.Name}\n{s.Content}"));
        parts.Add($"Available skill frameworks to execute the instruction:\n{skillsBlock}");
    }

    parts.Add($"You will execute the following instruction:\n{SynthesisInstruction}");

    return [
        new ChatMessage(MessageRole.System, string.Join("\n\n", parts)),
        new ChatMessage(MessageRole.User, userContent)
    ];
}
```

The `SelectViaLlmAsync` method remains for future dynamic selection. No other changes to `CommandHandlerBase`.

---

## Step 9 — Simplify `ObjectiveSynthesisHandler`

**File:** `src/AgentFramework.Core/Agent/Steps/CODESteps/KickoffChain/Handlers/ObjectiveSynthesisHandler.cs`

Remove the explicit `ISkillResolver` and the hardcoded `["kickoff-context"]` resolution. Skills are now pre-loaded into `stepCtx.Skills` by `PipelineCode` at construction time. `BuildPrompt(userContent)` picks them up automatically via the `resolvedSkills` fallback added in Step 8.

```csharp
public ObjectiveSynthesisHandler(IStepPromptLayer? stepCtx = null)
    : base(stepCtx, Instruction) { }

public override async Task<HandlerExchange> ExecuteAiCommandAsync(
    IReadOnlyList<HandlerExchange> context,
    IAgentRunContext? contextAgent,
    ISessionWriter writer,
    IChatClient? chatClient,
    CancellationToken ct)
{
    // ... build userContent (unchanged) ...

    // Skills are in stepCtx.Skills — BuildPrompt picks them up automatically
    var messages = BuildPrompt(userContent);
    var json = await chatClient!.SendHandlerAsync(messages, Schema, root => root.GetRawText(), ct);

    return new HandlerExchange(
        GetType().Name,
        new HandlerContent(context.LastOutput() ?? "[start]", json),
        new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
}
```

---

## Step 10 — Simplify `KickoffStep` constructor

**File:** `src/AgentFramework.Core/Agent/Steps/CODESteps/KickoffStep.cs`

Remove `ISkillResolver? skillResolver` parameter — it was only passed to `ObjectiveSynthesisHandler`, which no longer needs it.

```csharp
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
```

Remove the `_skillResolver` field. Update chain construction:

```csharp
var chain = new KickoffContextChain(
    new CheckpointValidatorCommand(_stepContext),
    new MarkFileLoaderHandler(_markFileReader),
    new IterationEvaluatorHandler(),
    new QuestionTriageHandler(_markFileReader, _stepContext),
    new ObjectiveSynthesisHandler(_stepContext));    // no resolver
```

---

## Step 11 — Update `AgentAggregate`

**File:** `src/AgentFramework.Core/Agent/AgentAggregate.cs`

Three changes:

**A. Remove `Role` property and update constructor:**

```csharp
// REMOVE
public Role Role { get; protected set; } = default!;
public IStepPromptLayer promptContext { get; init; }

// ADD
protected IStepPromptLayer AgentContext { get; }

// BEFORE constructor
public AgentAggregate(TId id, Role role, string projectId, SessionMarkFilePaths markFilePaths)
{
    Id = id;
    Role = role;
    promptContext = ((IAgentPromptLayer)PromptContext.Empty).WithRole(role);
    Session = new AgentSession(projectId, markFilePaths);
}

// AFTER constructor
public AgentAggregate(TId id, IStepPromptLayer agentContext, string projectId, SessionMarkFilePaths markFilePaths)
{
    Id = id;
    AgentContext = agentContext;
    Session = new AgentSession(projectId, markFilePaths);
}
```

**B. Remove `IPipelineFactory` from `RunAsync`:**

```csharp
// BEFORE
public async Task<AgentRunResult> RunAsync(
    string userIntent,
    IChatClient chatClient,
    IDeliverableWriter deliverableWriter,
    IPipelineFactory pipelineFactory,
    CancellationToken ct = default)
{
    Pipeline = await pipelineFactory.CreatePipelineAsync(promptContext, ct);
    // ...
}

// AFTER
public async Task<AgentRunResult> RunAsync(
    string userIntent,
    IChatClient chatClient,
    IDeliverableWriter deliverableWriter,
    CancellationToken ct = default)
{
    // Pipeline is set at construction — no factory needed
    Session?.SetUserIntent(userIntent);
    // ... rest unchanged ...
}
```

**C. Remove the import of `IPipelineFactory` and `Role` from the using block.**

---

## Step 12 — Delete `IPipelineFactory` and `Role.cs`

**Delete:** `src/AgentFramework.Core/Agent/Ports/IPipelineFactory.cs`

**Delete:** `src/AgentFramework.Core/Agent/Role.cs`

Both `Role` and `Identity` classes are now replaced by the `RoleDefinition` and `Identity` records in `Agent/Prompts/RoleDefinition.cs`.

At this point the solution should compile cleanly (minus test projects and `UxPersona` which still reference old types).

---

## Step 13 — Create config records

**File:** `src/AgentFramework.Core/Agent/Steps/StepSkillConfig.cs` (NEW)

```csharp
namespace AgentFramework.Core.Agent.Steps;

/// <summary>
/// Serialisable configuration for a single step.
/// Future storage: one property on a CosmosDB document.
/// </summary>
public record StepSkillConfig(
    string Instructions,
    Gate Gate,
    IReadOnlyList<string> SkillNames);
```

**File:** `src/AgentFramework.Core/Agent/Steps/CodePipelineConfig.cs` (NEW)

```csharp
namespace AgentFramework.Core.Agent.Steps;

/// <summary>
/// Serialisable configuration for the CODE 5-phase pipeline.
/// Future storage: top-level CosmosDB document for an agent instance.
/// </summary>
public record CodePipelineConfig(
    StepSkillConfig Kickoff,
    StepSkillConfig Capture,
    StepSkillConfig Organize,
    StepSkillConfig Distill,
    StepSkillConfig Express);
```

---

## Step 14 — Create `PipelineCode`

**File:** `src/AgentFramework.Core/Agent/Steps/PipelineCode.cs` (NEW)

`PipelineCode` is a regular class (not static), injected via DI. Its single responsibility is to construct the 5-step CODE pipeline from a config, resolving skills asynchronously for each step.

```csharp
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Steps.CODESteps;

namespace AgentFramework.Core.Agent.Steps;

public class PipelineCode
{
    private readonly ISkillResolver   _skillResolver;
    private readonly IMarkFileReader? _markFileReader;

    public PipelineCode(ISkillResolver skillResolver, IMarkFileReader? markFileReader = null)
    {
        _skillResolver  = skillResolver ?? throw new ArgumentNullException(nameof(skillResolver));
        _markFileReader = markFileReader;
    }

    public async Task<StepPipeline> BuildAsync(
        IStepPromptLayer   agentContext,
        CodePipelineConfig config,
        CancellationToken  ct = default)
    {
        var kickoffCtx  = await ResolveStepContextAsync(agentContext, config.Kickoff.SkillNames,  ct);
        var captureCtx  = await ResolveStepContextAsync(agentContext, config.Capture.SkillNames,  ct);
        var organizeCtx = await ResolveStepContextAsync(agentContext, config.Organize.SkillNames, ct);
        var distillCtx  = await ResolveStepContextAsync(agentContext, config.Distill.SkillNames,  ct);
        var expressCtx  = await ResolveStepContextAsync(agentContext, config.Express.SkillNames,  ct);

        return new StepPipeline([
            new KickoffStep (kickoffCtx,  1, config.Kickoff.Instructions,  config.Kickoff.Gate,  _markFileReader),
            new CaptureStep (captureCtx,  2, config.Capture.Instructions,  config.Capture.Gate),
            new OrganizeStep(organizeCtx, 3, config.Organize.Instructions, config.Organize.Gate),
            new DistillStep (distillCtx,  4, config.Distill.Instructions,  config.Distill.Gate),
            new ExpressStep (expressCtx,  5, config.Express.Instructions,  config.Express.Gate),
        ]);
    }

    private async Task<IStepPromptLayer> ResolveStepContextAsync(
        IStepPromptLayer      agentContext,
        IReadOnlyList<string> skillNames,
        CancellationToken     ct)
    {
        if (skillNames.Count == 0) return agentContext;
        var skills = await _skillResolver.ResolveAsync(skillNames, ct);
        return agentContext.WithSkills(skills);
    }
}
```

**DI registration** (add to `ServiceCollectionExtensions`):
```csharp
services.AddScoped<PipelineCode>();
```

---

## Step 15 — Create `UxPersonaConfig` and update `UxPersona`

**File:** `src/AgentFramework.Domain/UxAgent/UxPersonaConfig.cs` (NEW)

```csharp
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

/// <summary>
/// Serialisable agent configuration.
/// Future: loaded from CosmosDB via IAgentRepository and passed to UxPersona.BuildAsync.
/// </summary>
public record UxPersonaConfig(
    string AgentId,
    string ProjectId,
    SessionMarkFilePaths MarkFilePaths,
    RoleDefinition Role,
    CodePipelineConfig Pipeline);
```

**File:** `src/AgentFramework.Domain/UxAgent/UxAgent.cs` (REWRITE)

```csharp
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

public class UxPersona : AgentAggregate<string>
{
    private UxPersona(
        string               agentId,
        IStepPromptLayer     agentContext,
        StepPipeline         pipeline,
        string               projectId,
        SessionMarkFilePaths markFilePaths)
        : base(agentId, agentContext, projectId, markFilePaths)
    {
        Pipeline = pipeline;
    }

    /// <summary>
    /// Primary factory. Config is serialisable — load from CosmosDB or construct inline.
    /// </summary>
    public static async Task<UxPersona> BuildAsync(
        UxPersonaConfig   config,
        PipelineCode      pipelineCode,
        CancellationToken ct = default)
    {
        var agentContext = ((IAgentPromptLayer)PromptContext.Empty).WithRole(config.Role);
        var pipeline     = await pipelineCode.BuildAsync(agentContext, config.Pipeline, ct);
        return new UxPersona(config.AgentId, agentContext, pipeline, config.ProjectId, config.MarkFilePaths);
    }
}
```

**Delete:** `src/AgentFramework.Domain/UxAgent/UxStepBuilder.cs` — `PipelineCode.BuildAsync` creates `new StepPipeline(steps)` directly.

---

## Step 16 — Create `TestData/Skills/okr-kickoff-strategy.md`

**File:** `tests/AgentFramework.Core.Tests/TestData/Skills/okr-kickoff-strategy.md` (NEW)

```markdown
---
name: okr-kickoff-strategy
description: Frame the session objective using OKR principles — Objective + Key Results.
---

# OKR Kickoff Strategy

Use OKR principles to sharpen the session objective into a measurable, outcome-oriented statement.

## Framework

**Objective** — Qualitative direction: what we want to achieve and why it matters.
**Key Results** — 2-3 quantitative outcomes that define success for this session.

## Steps

1. **Extract core intent** from user input and prior session checkpoint.

2. **Draft the Objective** using this template:
   > [Action verb] [domain deliverable] so that [stakeholder] can [outcome].
   Example: "Define validated persona cards so that the UX team can begin journey mapping."

3. **Define Key Results** — 2-3 measurable conditions that confirm the Objective was achieved:
   - Must be falsifiable (pass/fail at session end)
   - Scoped to one session
   - Reference concrete artifacts or counts
   Examples:
   - "≥3 distinct persona cards produced, each with a JTBD statement"
   - "All user types from the product description accounted for"
   - "Anti-persona identified and documented with exclusion rationale"

4. **Compose the Session Objective** by combining Objective + Key Results into a single statement:
   > [Objective]. Success = [KR1]; [KR2]; [KR3]. Stakes: [what is blocked without these].

5. **Validate** — all must pass before emitting:
   - [ ] Starts with an action verb
   - [ ] Specifies a deliverable (not a process)
   - [ ] Key Results are countable or binary
   - [ ] Stakes clause names a real downstream consequence

## Errors

| Condition | Response |
|---|---|
| Vague objective | Break down until a concrete deliverable is named |
| Missing stakes | Ask: "What is blocked if this session produces nothing?" |
| KR is a task not a result | Reframe: "X produced/validated" not "X worked on" |
| More than 3 KRs | Reduce — prioritise the 3 most falsifiable |
```

---

## Step 17 — Add `FlatFileSkillResolver` to test project

The test `TestData/Skills` folder uses flat `.md` files (`Kickoff-context.md`, `okr-kickoff-strategy.md`) rather than the `{name}/SKILL.md` subdirectory format expected by `FileSkillResolver`. Add a test-only resolver.

**File:** `tests/AgentFramework.Core.Tests/TestHelpers/FlatFileSkillResolver.cs` (NEW)

```csharp
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Tests.TestHelpers;

/// <summary>
/// Resolves skills from flat {basePath}/{skillName}.md files.
/// For use in tests only — production code uses FileSkillResolver.
/// </summary>
internal sealed class FlatFileSkillResolver : ISkillResolver
{
    private readonly string _basePath;

    public FlatFileSkillResolver(string basePath) => _basePath = basePath;

    public Task<IReadOnlyList<SkillSummary>> GetCatalogAsync(CancellationToken ct = default)
    {
        var summaries = Directory.GetFiles(_basePath, "*.md")
            .Select(f => SkillParser.ParseSummaryFromMarkdown(File.ReadAllText(f)))
            .ToList();
        return Task.FromResult<IReadOnlyList<SkillSummary>>(summaries);
    }

    public Task<IReadOnlyList<Skill>> ResolveAsync(
        IReadOnlyList<string> skillNames, CancellationToken ct = default)
    {
        var skills = skillNames
            .Select(name => Path.Combine(_basePath, $"{name}.md"))
            .Where(File.Exists)
            .Select(path => SkillParser.ParseFromMarkdown(File.ReadAllText(path)))
            .ToList();
        return Task.FromResult<IReadOnlyList<Skill>>(skills);
    }
}
```

---

## Step 18 — Update `TestSteps.cs`

`DefaultSteps` and `DefaultPipeline` remain for any tests that instantiate steps directly (e.g. `KickoffStepTests.CreateStep()`). Add `DefaultConfig()` and `DefaultPipelineCode()` factory helpers for agent-construction tests.

**File:** `tests/AgentFramework.Core.Tests/TestSteps.cs` (UPDATE)

```csharp
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Agent.Steps.CODESteps;
using AgentFramework.Core.Tests.TestHelpers;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

internal static class TestSteps
{
    private const string SkillsBasePath = "TestData/Skills";

    // -------------------------------------------------------
    // Raw step construction (for unit tests that test steps directly)
    // -------------------------------------------------------

    public static AgentStep[] DefaultSteps(IStepPromptLayer? stepContext = null)
    {
        var ctx = stepContext ?? PromptContext.Empty;
        return
        [
            new KickoffStep(
                stepContext:  ctx,
                stepNumber:   1,
                instructions: "Session Objective. Parse product description.",
                gate:         new Gate("Objective confirmed")),

            new CaptureStep(
                stepContext:  ctx,
                stepNumber:   2,
                instructions: "Hunt for user types · goals · pain points · behavioral patterns.",
                gate:         new Gate("≥3 user-type islands")),

            new OrganizeStep(
                stepContext:  ctx,
                stepNumber:   3,
                instructions: "Cluster by person → proto-persona. Merge overlapping clusters.",
                gate:         new Gate("2-5 ranked candidates")),

            new DistillStep(
                stepContext:  ctx,
                stepNumber:   4,
                instructions: "Produce Persona Cards per template. JTBD for each.",
                gate:         new Gate("All → Card or Concern")),

            new ExpressStep(
                stepContext:  ctx,
                stepNumber:   5,
                instructions: "Write cards. Emit relay. Record token usage.",
                gate:         new Gate("Session + Cards + Relay + Token Usage logged")),
        ];
    }

    // -------------------------------------------------------
    // Config-based construction (for agent-level tests)
    // -------------------------------------------------------

    public static CodePipelineConfig DefaultConfig() => new(
        Kickoff:  new StepSkillConfig(
            Instructions: "Session Objective. Parse product description.",
            Gate:         new Gate("Objective confirmed"),
            SkillNames:   ["Kickoff-context", "okr-kickoff-strategy"]),

        Capture:  new StepSkillConfig(
            Instructions: "Hunt for user types · goals · pain points · behavioral patterns.",
            Gate:         new Gate("≥3 user-type islands"),
            SkillNames:   ["autonomous-capture"]),

        Organize: new StepSkillConfig(
            Instructions: "Cluster by person → proto-persona. Merge overlapping clusters.",
            Gate:         new Gate("2-5 ranked candidates"),
            SkillNames:   ["strategic-organize"]),

        Distill:  new StepSkillConfig(
            Instructions: "Produce Persona Cards per template. JTBD for each.",
            Gate:         new Gate("All → Card or Concern"),
            SkillNames:   ["expert-distill"]),

        Express:  new StepSkillConfig(
            Instructions: "Write cards. Emit relay. Record token usage.",
            Gate:         new Gate("Session + Cards + Relay + Token Usage logged"),
            SkillNames:   ["express-relay"]));

    public static PipelineCode DefaultPipelineCode() =>
        new(new FlatFileSkillResolver(SkillsBasePath));

    public static UxPersonaConfig DefaultUxConfig(RoleDefinition role) => new(
        AgentId:      "ux-persona-test",
        ProjectId:    "test-proj",
        MarkFilePaths: new SessionMarkFilePaths("UX", "outputs/contextAgent"),
        Role:          role,
        Pipeline:      DefaultConfig());
}
```

---

## Step 19 — Update `KickoffStepTests.cs`

The schema and `ParseResult` tests (`CreateStep()`) are unchanged — they test `KickoffStep` directly and don't need `UxPersona`.

The agent-level tests that use `CreateAgent()` / `CreateAgentWithSkills()` become async.

**Key changes:**

```csharp
// BEFORE
private static UxPersona CreateAgent()
{
    var markdown = File.ReadAllText(TestDataPath);
    var role = RoleParser.ParseFromMarkdown(markdown);
    IStepPromptLayer ctx = ((IAgentPromptLayer)PromptContext.Empty).WithRole(role);
    return new UxPersona(role, TestSteps.DefaultSteps(ctx), "test-proj", TestMarkFilePaths);
}

// AFTER
private static async Task<UxPersona> CreateAgentAsync()
{
    var markdown = File.ReadAllText(TestDataPath);
    var role     = RoleParser.ParseFromMarkdown(markdown);  // returns RoleDefinition
    var config   = TestSteps.DefaultUxConfig(role);
    return await UxPersona.BuildAsync(config, TestSteps.DefaultPipelineCode());
}
```

```csharp
// BEFORE
private static UxPersona CreateAgentWithSkills()  // same as CreateAgent — no actual difference
{
    // ... identical body ...
}

// AFTER — no separate method needed; DefaultUxConfig already includes skill names
// Tests that previously called CreateAgentWithSkills() call CreateAgentAsync() instead
```

All test methods that previously called `CreateAgent()` become `async Task` and call `await CreateAgentAsync()`.

Example before/after for `ApplyTo_UpdatesSessionObjective`:

```csharp
// BEFORE
[Fact]
public void ApplyTo_UpdatesSessionObjective()
{
    var agent = CreateAgent();
    // ...
}

// AFTER
[Fact]
public async Task ApplyTo_UpdatesSessionObjective()
{
    var agent = await CreateAgentAsync();
    // ...
}
```

---

## Step 20 — Update `UxPersonaTests.cs`

Remove `TestPipelineFactory` inner class — `PipelineCode` replaces it.

**Factory tests** (`Factory_CreatesUxPersona_WithRoleLoadedFromMd`, `Factory_CreatesUxPersona_WithStepsLoadedFromMd`):

```csharp
// BEFORE
var agent = new UxPersona(RoleParser.ParseFromMarkdown(markdown), TestSteps.DefaultSteps(), "test-proj", ...);

// AFTER
var role   = RoleParser.ParseFromMarkdown(markdown);
var config = TestSteps.DefaultUxConfig(role);
var agent  = await UxPersona.BuildAsync(config, TestSteps.DefaultPipelineCode());
```

Make the test methods `async Task`.

**Integration tests** (`ExecuteFirstStep_WithAnthropicLlm_*`, `UxPersona_FullProcess_*`):

```csharp
// BEFORE — RunAsync takes IPipelineFactory
var runResult = await agent.RunAsync(
    "Analyze ...",
    chatClient,
    deliverableWriter,
    pipelineFactory);    // ← REMOVE

// AFTER — RunAsync has no factory; pipeline is built via BuildAsync
var role   = RoleParser.ParseFromMarkdown(markdown);
var config = TestSteps.DefaultUxConfig(role) with
{
    Pipeline = TestSteps.DefaultConfig()  // or provide a production config
};
var agent = await UxPersona.BuildAsync(config, new PipelineCode(
    new FileSkillResolver(".claude/skills"),  // or TestData/Skills for tests
    null));

agent.SetUserIntent("Analyze a college athletic recruiting platform...");
var runResult = await agent.RunAsync("...", chatClient, deliverableWriter);
```

The `TestPipelineFactory` inner class is deleted.

---

## Step 21 — Final compile and verification

Run in order:

```bash
# from repo root
cd src/multy-agent
dotnet build --no-incremental 2>&1 | grep -E "error|warning"
```

Expected errors at this point: none (all breaking changes are addressed in Steps 1–20).

```bash
dotnet test tests/AgentFramework.Core.Tests --filter "Category!=Integration" --no-build
```

All unit tests should pass. Integration tests require an Anthropic API key configured in `appsettings.local.json` and are skipped automatically when the key is absent.

---

## Files Created

| File | Action |
|---|---|
| `src/AgentFramework.Core/Agent/Prompts/RoleDefinition.cs` | NEW |
| `src/AgentFramework.Core/Agent/Steps/StepSkillConfig.cs` | NEW |
| `src/AgentFramework.Core/Agent/Steps/CodePipelineConfig.cs` | NEW |
| `src/AgentFramework.Core/Agent/Steps/PipelineCode.cs` | NEW |
| `src/AgentFramework.Domain/UxAgent/UxPersonaConfig.cs` | NEW |
| `tests/AgentFramework.Core.Tests/TestHelpers/FlatFileSkillResolver.cs` | NEW |
| `tests/AgentFramework.Core.Tests/TestData/Skills/okr-kickoff-strategy.md` | NEW |

## Files Modified

| File | Change Summary |
|---|---|
| `src/AgentFramework.Core/Agent/RoleParser.cs` | Returns `RoleDefinition`; adds `using Prompts` |
| `src/AgentFramework.Core/Agent/Prompts/IAgentPromptLayer.cs` | `WithRole(RoleDefinition)`; adds `Role` property |
| `src/AgentFramework.Core/Agent/Prompts/IStepPromptLayer.cs` | Adds `RoleDefinition? Role` property |
| `src/AgentFramework.Core/Agent/Prompts/PromptContext.cs` | `RoleDefinition? Role` stored; `RolePrompt` computed |
| `src/AgentFramework.Core/Agent/Steps/Skill.cs` | `Instructions` → `Content` |
| `src/AgentFramework.Core/Agent/Steps/SkillParser.cs` | `instructions` → `content` |
| `src/AgentFramework.Core/Agent/Steps/CODESteps/KickoffChain/Handlers/CommandHandlerBase.cs` | `s.Content`; `BuildPrompt` falls back to `_stepContext.Skills` |
| `src/AgentFramework.Core/Agent/Steps/CODESteps/KickoffChain/Handlers/ObjectiveSynthesisHandler.cs` | Remove `ISkillResolver`; call `BuildPrompt(userContent)` |
| `src/AgentFramework.Core/Agent/Steps/CODESteps/KickoffStep.cs` | Remove `ISkillResolver` param; update chain construction |
| `src/AgentFramework.Core/Agent/AgentAggregate.cs` | Remove `Role`; new constructor; `RunAsync` sans factory |
| `src/AgentFramework.Infrastructure/ServiceCollectionExtensions.cs` | Register `PipelineCode` as scoped |
| `src/AgentFramework.Domain/UxAgent/UxAgent.cs` | Private constructor + `BuildAsync` |
| `tests/AgentFramework.Core.Tests/TestSteps.cs` | Add `DefaultConfig`, `DefaultPipelineCode`, `DefaultUxConfig` |
| `tests/AgentFramework.Core.Tests/KickoffStepTests.cs` | Async agent construction; remove `CreateAgentWithSkills` |
| `tests/AgentFramework.Core.Tests/UxPersonaTests.cs` | Remove `TestPipelineFactory`; async `BuildAsync` pattern |

## Files Deleted

| File | Reason |
|---|---|
| `src/AgentFramework.Core/Agent/Role.cs` | Replaced by `RoleDefinition` record |
| `src/AgentFramework.Core/Agent/Ports/IPipelineFactory.cs` | Replaced by `PipelineCode.BuildAsync` |
| `src/AgentFramework.Domain/UxAgent/UxStepBuilder.cs` | Replaced by `PipelineCode`; `StepPipeline` constructed directly |

---

## Design Principles This Enforces

**Three-layer ownership** — Aggregate holds the role; Step holds resolved skills; Command assembles the prompt. Each layer knows exactly what it owns.

**Configuration over construction** — `UxPersonaConfig` + `CodePipelineConfig` + `StepSkillConfig` are plain records. Future: store in CosmosDB, load via `IAgentRepository`, pass to `UxPersona.BuildAsync`. Zero code changes at the application layer.

**One assembly point** — `CommandHandlerBase.BuildPrompt` is the single visible, overridable template that turns role + skills + instruction into LLM messages. No hidden assembly paths.

**Skills resolved once at construction** — `PipelineCode.BuildAsync` resolves all skill names before any LLM call. Step contexts are immutable after construction. No lazy resolution inside handlers.
