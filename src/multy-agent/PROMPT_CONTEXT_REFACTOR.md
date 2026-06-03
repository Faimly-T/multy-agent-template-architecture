# PromptContext Refactor — Implementation Plan

## Context

This plan introduces a **layered `PromptContext` model** with Type State interfaces, a simplified `Skill` record, and a dynamic `ISkillResolver` with two selection patterns. Execute steps in order — the project should build after each one.

---

## Architecture in One Page

### Problem being fixed

`AgentStep.BuildMessages(IAgentRunContext?)` and `CommandHandlerBase.BuildMessages(string, IAgentRunContext?)` duplicate the same context composition logic. `AgentStep.BuildMessages` is dead code in chain-based steps (called but result discarded). Skills are loaded once at pipeline build time and distributed — eager and centralised.

### What replaces it

**`PromptContext`** — a single immutable record implementing three capability interfaces. Each architectural layer receives only the interface for its level:

| Interface | Received by | Can do |
|---|---|---|
| `IAgentPromptLayer` | `AgentAggregate` | `WithRole(Role)` only |
| `IStepPromptLayer` | `AgentStep` + `CommandHandlerBase` | `WithSkills(...)`, `RoleOnly`, `ForCommand` |
| `ICommandPromptLayer` | Created internally inside `ResolveAndBuildAsync` | `BuildMessages(instruction, userContent)` |

**`ISkillResolver`** — replaces `ISkillProvider`. Two methods: `GetCatalogAsync` (lightweight discovery) and `ResolveAsync` (full instructions by name). Skills are resolved per command at execution time, not at pipeline build time.

**Two skill selection patterns per command** — both optional, resolver is optional:
- **Pattern A — Explicit**: command overrides `SkillNames` with exact names → `ResolveAsync` fetches them
- **Pattern B — LLM-driven**: command sets `AutoSelectSkills = true` → catalog presented to LLM via existing `IChatClient` → selected names passed to `ResolveAsync`

### Skill model

`Skill` is a plain record: `Name`, `Description`, `Instructions`. No categorisation, no enum. Strategy is encoded in the name: `kickoff-context-usingOKR`, `kickoff-context-usingSMART`, etc. Order of skills in `BuildMessages` follows the order they were resolved.

### Context flow

```
AgentAggregate
  → IAgentPromptLayer.Empty.WithRole(Role)
  → IStepPromptLayer  [Role set, lazy cached]
  → exposed via IAgentRunContext.PromptContext

AgentStep.BuildStepContext(context)
  → context.PromptContext          [IStepPromptLayer, Role only — no skills at step level]
  → passed to commands via constructor

CommandHandlerBase.ResolveAndBuildAsync(userContent, chatClient, ct)
  → ResolveSkillsAsync()           [Pattern A or B — fetches only what this command needs]
  → stepCtx.WithSkills(skills).ForCommand  [ICommandPromptLayer — ephemeral, local only]
  → .BuildMessages(SynthesisInstruction, userContent)
  → IReadOnlyList<ChatMessage>     [only place ChatMessages are constructed]
```

---

## Files Changed Summary

| File | Action |
|---|---|
| `Agent/Prompts/IAgentPromptLayer.cs` | **CREATE** |
| `Agent/Prompts/IStepPromptLayer.cs` | **CREATE** |
| `Agent/Prompts/ICommandPromptLayer.cs` | **CREATE** |
| `Agent/Prompts/PromptContext.cs` | **CREATE** |
| `Agent/Steps/CODESteps/DefaultStepCommand.cs` | **CREATE** |
| `Agent/Steps/Skill.cs` | MODIFY — remove `SkillRole`, simplify record |
| `Agent/Steps/SkillSummary.cs` | **CREATE** |
| `Agent/Steps/SkillParser.cs` | MODIFY — remove `role` parsing |
| `Agent/Ports/ISkillProvider.cs` | DELETE — replaced by `ISkillResolver` |
| `Agent/Ports/ISkillResolver.cs` | **CREATE** |
| `Agent/IAgentRunContext.cs` | MODIFY — replace `Role` with `PromptContext` |
| `Agent/AgentAggregate.cs` | MODIFY — add cache field, implement `PromptContext` |
| `Agent/Steps/AgentStep.cs` | MODIFY — remove `BuildMessages`, add `BuildStepContext`, upgrade Skills |
| `Agent/Steps/CODESteps/KickoffChain/Handlers/CommandHandlerBase.cs` | MODIFY — new constructor + skill resolution |
| `Agent/Steps/CODESteps/KickoffChain/Handlers/CheckpointValidatorHandler.cs` | MODIFY — new constructor |
| `Agent/Steps/CODESteps/KickoffChain/Handlers/ObjectiveSynthesisHandler.cs` | MODIFY — new constructor + SkillNames |
| `Agent/Steps/CODESteps/KickoffChain/Handlers/QuestionTriageHandler.cs` | MODIFY — extend `CommandHandlerBase` |
| `Agent/Steps/CODESteps/KickoffStep.cs` | MODIFY — remove dead code, update chain assembly |
| `Domain/UxAgent/UxStepBuilder.cs` | MODIFY — remove skill attachment (skills now per-command) |
| `Domain/UxAgent/UxAgent.cs` | MODIFY — remove `skills` parameter |
| `Infrastructure/Skills/FileSkillProvider.cs` | MODIFY — rename to `FileSkillResolver`, implement `ISkillResolver` |

---

## Execution Order

---

## Step 1 — Create `Agent/Prompts/` folder with four files

### `Agent/Prompts/IAgentPromptLayer.cs`

```csharp
namespace AgentFramework.Core.Agent.Prompts;

public interface IAgentPromptLayer
{
    static IAgentPromptLayer Empty => PromptContext.Empty;
    IStepPromptLayer WithRole(Role role);
}
```

### `Agent/Prompts/IStepPromptLayer.cs`

```csharp
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Prompts;

public interface IStepPromptLayer
{
    IStepPromptLayer    WithSkills(IReadOnlyList<Skill> skills);
    IStepPromptLayer    WithoutSkills();
    ICommandPromptLayer RoleOnly   { get; }
    ICommandPromptLayer ForCommand { get; }
}
```

### `Agent/Prompts/ICommandPromptLayer.cs`

```csharp
using AgentFramework.Core.Agent.Conversation;

namespace AgentFramework.Core.Agent.Prompts;

public interface ICommandPromptLayer
{
    IReadOnlyList<ChatMessage> BuildMessages(string instruction, string userContent);
}
```

### `Agent/Prompts/PromptContext.cs`

```csharp
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Prompts;

public sealed record PromptContext
    : IAgentPromptLayer, IStepPromptLayer, ICommandPromptLayer
{
    private string?              RolePrompt { get; init; }
    private IReadOnlyList<Skill> Skills     { get; init; } = [];

    public static PromptContext Empty => new();

    // IAgentPromptLayer
    IStepPromptLayer IAgentPromptLayer.WithRole(Role role)
        => this with { RolePrompt = role.BuildRolePrompt };

    // IStepPromptLayer
    IStepPromptLayer IStepPromptLayer.WithSkills(IReadOnlyList<Skill> skills)
        => this with { Skills = skills };

    IStepPromptLayer IStepPromptLayer.WithoutSkills()
        => this with { Skills = [] };

    ICommandPromptLayer IStepPromptLayer.RoleOnly
        => this with { Skills = [] };

    ICommandPromptLayer IStepPromptLayer.ForCommand => this;

    // ICommandPromptLayer — the only place ChatMessage lists are built
    IReadOnlyList<ChatMessage> ICommandPromptLayer.BuildMessages(
        string instruction, string userContent)
    {
        var parts = new List<string>();

        if (RolePrompt is not null)
            parts.Add(RolePrompt);

        foreach (var skill in Skills)
            parts.Add($"### Skill: {skill.Name}\n{skill.Instructions}");

        if (!string.IsNullOrWhiteSpace(instruction))
            parts.Add(instruction);

        return [
            new ChatMessage(MessageRole.System, string.Join("\n\n", parts)),
            new ChatMessage(MessageRole.User,   userContent)
        ];
    }
}
```

---

## Step 2 — Simplify `Skill`, add `SkillSummary`, update `SkillParser`

### `Agent/Steps/Skill.cs` — replace entire file

```csharp
namespace AgentFramework.Core.Agent.Steps;

/// <summary>
/// A context layer injected into the LLM prompt for a specific command.
/// Skills are not step instructions — they are domain guidance documents.
/// Strategy is encoded in the name: kickoff-context-usingOKR, kickoff-context-usingSMART, etc.
/// </summary>
public record Skill(string Name, string Description, string Instructions);
```

### `Agent/Steps/SkillSummary.cs` — create new file

```csharp
namespace AgentFramework.Core.Agent.Steps;

/// <summary>
/// Lightweight view of a Skill used for catalog discovery and LLM-driven selection.
/// Contains only Name and Description — Instructions are not loaded until ResolveAsync is called.
/// </summary>
public record SkillSummary(string Name, string Description);
```

### `Agent/Steps/SkillParser.cs` — replace entire file

```csharp
using System.Text.RegularExpressions;

namespace AgentFramework.Core.Agent.Steps;

public static class SkillParser
{
    public static Skill ParseFromMarkdown(string markdown)
    {
        var name        = ExtractFrontmatter(markdown, "name");
        var description = ExtractFrontmatter(markdown, "description");
        var instructions = ExtractBody(markdown);
        return new Skill(name, description, instructions);
    }

    public static SkillSummary ParseSummaryFromMarkdown(string markdown)
    {
        var name        = ExtractFrontmatter(markdown, "name");
        var description = ExtractFrontmatter(markdown, "description");
        return new SkillSummary(name, description);
    }

    private static string ExtractFrontmatter(string md, string field)
    {
        var match = Regex.Match(md, $@"^{field}:\s*(.+)$", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static string ExtractBody(string md)
    {
        var match = Regex.Match(md, @"---\s*\n[\s\S]*?---\s*\n(.+)", RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : md.Trim();
    }
}
```

---

## Step 3 — Replace `ISkillProvider` with `ISkillResolver`

### Delete `Agent/Ports/ISkillProvider.cs`

Remove the file entirely.

### Create `Agent/Ports/ISkillResolver.cs`

```csharp
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Ports;

/// <summary>
/// Data-only port for skill access. No selection logic — that belongs to the command.
///
/// Two operations:
///   GetCatalogAsync  — lightweight discovery (name + description only, safe to send to LLM)
///   ResolveAsync     — full skill instructions for selected names, in the order requested
/// </summary>
public interface ISkillResolver
{
    Task<IReadOnlyList<SkillSummary>> GetCatalogAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Skill>> ResolveAsync(
        IReadOnlyList<string> skillNames,
        CancellationToken     ct = default);
}
```

---

## Step 4 — Update `IAgentRunContext.cs` — replace entire file

```csharp
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent;

public interface IAgentRunContext
{
    /// <summary>
    /// Prompt context with the agent Role already set by the Aggregate.
    /// Steps and commands enrich this with Skills at execution time.
    /// </summary>
    IStepPromptLayer PromptContext { get; }

    AgentSession?                   Session           { get; }
    IReadOnlyList<Question>         Questions         { get; }
    IReadOnlyList<Decision>         Decisions         { get; }
    IReadOnlyList<Deliverable>      Deliverables      { get; }
    IReadOnlyList<StepConversation> StepConversations { get; }
}
```

---

## Step 5 — Update `AgentAggregate.cs`

### Add using at top of file

```csharp
using AgentFramework.Core.Agent.Prompts;
```

### Add private cache field — place after existing private fields near top of class

```csharp
private IStepPromptLayer? _basePromptContext;
```

### Replace the `IAgentRunContext` explicit implementation block

Find:
```csharp
// ==========================================================
// IAgentRunContext
// ==========================================================

AgentSession? IAgentRunContext.Session => Session;
IReadOnlyList<Question> IAgentRunContext.Questions => _questions.AsReadOnly();
IReadOnlyList<Decision> IAgentRunContext.Decisions => _decisions.AsReadOnly();
IReadOnlyList<Deliverable> IAgentRunContext.Deliverables => _deliverables.AsReadOnly();
IReadOnlyList<StepConversation> IAgentRunContext.StepConversations => _stepConversations.Steps;
```

Replace with:
```csharp
// ==========================================================
// IAgentRunContext
// ==========================================================

/// <summary>
/// Lazily builds the base prompt context from the Aggregate's Role.
/// Cached — role never changes mid-run.
/// Exposed as IStepPromptLayer so Steps and Commands can add Skills.
/// </summary>
IStepPromptLayer IAgentRunContext.PromptContext
    => _basePromptContext ??=
        ((IAgentPromptLayer)PromptContext.Empty).WithRole(Role);

AgentSession? IAgentRunContext.Session => Session;
IReadOnlyList<Question> IAgentRunContext.Questions => _questions.AsReadOnly();
IReadOnlyList<Decision> IAgentRunContext.Decisions => _decisions.AsReadOnly();
IReadOnlyList<Deliverable> IAgentRunContext.Deliverables => _deliverables.AsReadOnly();
IReadOnlyList<StepConversation> IAgentRunContext.StepConversations => _stepConversations.Steps;
```

### Remove `ISkillProvider` from `RunAsync` signature

Find:
```csharp
public async Task<AgentRunResult> RunAsync(
    string userIntent,
    IChatClient chatClient,
    ISkillProvider skillProvider,
    IDeliverableWriter deliverableWriter,
    IPipelineFactory pipelineFactory,
    CancellationToken ct = default)
{
    Pipeline = await pipelineFactory.CreatePipelineAsync(Role, skillProvider, ct);
```

Replace with:
```csharp
public async Task<AgentRunResult> RunAsync(
    string userIntent,
    IChatClient chatClient,
    IDeliverableWriter deliverableWriter,
    IPipelineFactory pipelineFactory,
    CancellationToken ct = default)
{
    Pipeline = await pipelineFactory.CreatePipelineAsync(Role, ct);
```

---

## Step 6 — Update `AgentStep.cs` — replace entire file

```csharp
using System.Text.Json;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps;

public abstract class AgentStep
{
    public string Name         => GetType().Name;
    public int    StepNumber   { get; }
    public string SkillName    { get; }
    public string Instructions { get; }
    public Gate   Gate         { get; }
    public virtual int MaxContextTokenBudget => 4096;

    public abstract string JsonResponseSchema { get; }

    protected AgentStep(int stepNumber, string skillName, string instructions, Gate gate)
    {
        StepNumber   = stepNumber;
        SkillName    = skillName;
        Instructions = instructions;
        Gate         = gate;
    }

    public virtual string BuildContext(IAgentRunContext? context) => string.Empty;

    public abstract StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied);

    /// <summary>
    /// Builds the step-level prompt context: Role only (from Aggregate via context).
    /// Skills are NOT attached at step level — each command resolves its own skills
    /// at execution time via ISkillResolver.
    /// </summary>
    protected IStepPromptLayer BuildStepContext(IAgentRunContext? context)
        => context?.PromptContext
           ?? ((IAgentPromptLayer)PromptContext.Empty).WithRole(null!);

    /// <summary>
    /// Default execution for simple (single-command) steps.
    /// Override in steps that use a chain or other multi-command pattern.
    /// </summary>
    public virtual async Task<StepResult?> ExecuteStepAsync(
        IAgentRunContext? context,
        ISessionWriter    writer,
        IChatClient       chatClient,
        CancellationToken ct = default)
    {
        var stepCtx = BuildStepContext(context);
        var command = new CODESteps.DefaultStepCommand(stepCtx, Instructions, this);
        var exchange = await command.ExecuteAiCommandAsync([], context, writer, chatClient, ct);

        using var doc = JsonDocument.Parse(exchange.Content.Output);
        var gateSatisfied = !doc.RootElement.TryGetProperty("gateSatisfied", out var gs)
                            || gs.GetBoolean();

        var result = ParseResult(doc.RootElement, exchange.Content.Output, gateSatisfied);
        writer.RecordStepExchange(StepNumber, Name, [], exchange.Content.Output);
        return result;
    }
}
```

> `BuildMessages(IAgentRunContext?)` removed. `Skill? Skill`, `AttachSkill`, `AttachSkills` removed — skills are no longer held at step level.

---

## Step 7 — Update `IPipelineFactory.cs`

Remove `ISkillProvider` from the signature.

Replace entire file:

```csharp
namespace AgentFramework.Core.Agent.Ports;

public interface IPipelineFactory
{
    Task<Steps.StepPipeline> CreatePipelineAsync(Role role, CancellationToken ct = default);
}
```

---

## Step 8 — Create `DefaultStepCommand.cs`

Create `Agent/Steps/CODESteps/DefaultStepCommand.cs`:

```csharp
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain;
using AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain.Handlers;

namespace AgentFramework.Core.Agent.Steps.CODESteps;

/// <summary>
/// Default single-command execution used by simple steps (Capture, Organize, Distill, Express).
/// Steps with chains or other patterns override ExecuteStepAsync and never use this.
/// No skill resolver — simple steps use their instructions only.
/// </summary>
internal sealed class DefaultStepCommand : CommandHandlerBase
{
    private readonly string    _instructions;
    private readonly AgentStep _step;

    public DefaultStepCommand(
        IStepPromptLayer? stepCtx,
        string            instructions,
        AgentStep         step)
        : base(stepCtx)
    {
        _instructions = instructions;
        _step         = step;
    }

    protected override string SynthesisInstruction => _instructions;

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext?              contextAgent,
        ISessionWriter                 writer,
        IChatClient?                   chatClient,
        CancellationToken              ct)
    {
        var userContent = _step.BuildContext(contextAgent);
        var schema      = $"""

            ### Required Response Format
            Respond ONLY with a JSON object matching this schema:
            ```json
            {_step.JsonResponseSchema}
            ```
            Gate: {_step.Gate.Description}
            """;

        var fullUser = string.IsNullOrWhiteSpace(userContent)
            ? schema
            : $"{userContent}\n\n{schema}";

        var messages = await ResolveAndBuildAsync(fullUser, chatClient, ct);
        var result   = await chatClient!.SendAsync(messages, _step, ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(context.LastOutput() ?? "[start]", result.Output),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }
}
```

---

## Step 9 — Update `CommandHandlerBase.cs` — replace entire file

```csharp
using System.Text.Json;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain.Handlers;

/// <summary>
/// Base for all command handlers in any step's execution pipeline.
///
/// Receives IStepPromptLayer (Role already set by Aggregate).
/// Skills are resolved lazily at execution time via ISkillResolver — not pre-loaded.
///
/// Two skill selection patterns:
///   Pattern A — Explicit: override SkillNames with exact skill names → ResolveAsync
///   Pattern B — LLM-driven: set AutoSelectSkills = true → catalog presented to LLM
///               via the same IChatClient passed to ExecuteAiCommandAsync → selected names
///               passed to ResolveAsync
///
/// IAgentRunContext in ExecuteAiCommandAsync is for SESSION DATA only
/// (checkpoints, questions, history) — never for prompt building.
/// </summary>
internal abstract class CommandHandlerBase(
    IStepPromptLayer? stepCtx  = null,
    ISkillResolver?   resolver = null) : ICommandHandler
{
    protected abstract string SynthesisInstruction { get; }

    // Pattern A — command knows exactly which skills it needs (no LLM)
    protected virtual IReadOnlyList<string> SkillNames => [];

    // Pattern B — LLM selects from the full catalog using the existing chatClient
    protected virtual bool AutoSelectSkills => false;

    /// <summary>
    /// Resolves skills (Pattern A or B), enriches the step context, and builds ChatMessages.
    /// This is the only place in the codebase where ChatMessage lists are constructed.
    /// </summary>
    protected async Task<IReadOnlyList<ChatMessage>> ResolveAndBuildAsync(
        string            userContent,
        IChatClient?      chatClient,
        CancellationToken ct)
    {
        var skills = await ResolveSkillsAsync(chatClient, ct);

        return (stepCtx ?? PromptContext.Empty)
            .WithSkills(skills)
            .ForCommand
            .BuildMessages(SynthesisInstruction, userContent);
    }

    private async Task<IReadOnlyList<Skill>> ResolveSkillsAsync(
        IChatClient? chatClient, CancellationToken ct)
    {
        if (resolver is null) return [];

        // Pattern A — explicit names, no LLM involved
        if (!AutoSelectSkills && SkillNames.Count > 0)
            return await resolver.ResolveAsync(SkillNames, ct);

        // Pattern B — LLM picks from catalog using the existing chatClient
        if (AutoSelectSkills && chatClient is not null)
        {
            var catalog  = await resolver.GetCatalogAsync(ct);
            var selected = await SelectViaLlmAsync(catalog, chatClient, ct);
            return await resolver.ResolveAsync(selected, ct);
        }

        return [];
    }

    /// <summary>
    /// Pattern B implementation — presents catalog to LLM, returns selected skill names.
    /// Uses the same IChatClient already flowing through ExecuteAiCommandAsync.
    /// </summary>
    private async Task<IReadOnlyList<string>> SelectViaLlmAsync(
        IReadOnlyList<SkillSummary> catalog,
        IChatClient                 chatClient,
        CancellationToken           ct)
    {
        var catalogText = string.Join("\n",
            catalog.Select(s => $"- {s.Name}: {s.Description}"));

        const string SelectionSchema = """
            { "selected": ["skill-name-1", "skill-name-2"] }
            """;

        const string SelectionInstruction =
            "From the available skills listed, select which ones apply to the current step context. " +
            "Respond with a JSON object containing a 'selected' array of skill names only. " +
            "Return an empty array if no skills are relevant.";

        var messages = (stepCtx ?? PromptContext.Empty)
            .ForCommand
            .BuildMessages(
                instruction: SelectionInstruction,
                userContent: $"Available skills:\n{catalogText}");

        return await chatClient.SendHandlerAsync(
            messages,
            SelectionSchema,
            root => root.TryGetProperty("selected", out var arr)
                ? arr.EnumerateArray().Select(e => e.GetString() ?? string.Empty)
                     .Where(s => !string.IsNullOrWhiteSpace(s))
                     .ToList()
                : (IReadOnlyList<string>)[],
            ct);
    }

    public abstract Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext?              contextAgent,
        ISessionWriter                 writer,
        IChatClient?                   chatClient,
        CancellationToken              ct);
}
```

---

## Step 10 — Update chain handlers

### `CheckpointValidatorHandler.cs`

Change constructor:
```csharp
// FROM:
internal class CheckpointValidatorCommand(Skill? skill = null) : CommandHandlerBase(skill)

// TO:
internal class CheckpointValidatorCommand(IStepPromptLayer? stepCtx = null)
    : CommandHandlerBase(stepCtx)
    // no resolver — checkpoint review needs no skills
```

Change `BuildMessages` call inside `ExecuteAiCommandAsync`:
```csharp
// FROM:
var messages = BuildMessages(userContent, contextAgent);

// TO:
var messages = await ResolveAndBuildAsync(userContent, chatClient, ct);
```

Add using, remove old using for `AgentFramework.Core.Agent.Steps`:
```csharp
using AgentFramework.Core.Agent.Prompts;
```

---

### `ObjectiveSynthesisHandler.cs`

Change constructor:
```csharp
// FROM:
internal sealed class ObjectiveSynthesisHandler(Skill? skill = null) : CommandHandlerBase(skill)

// TO:
internal sealed class ObjectiveSynthesisHandler(
    IStepPromptLayer? stepCtx  = null,
    ISkillResolver?   resolver = null)
    : CommandHandlerBase(stepCtx, resolver)
```

Add `SkillNames` or `AutoSelectSkills` property depending on whether this command uses
explicit skill selection or LLM-driven selection. For now, leave `SkillNames` empty (no skills)
and add them when skills are wired up:
```csharp
// Option A — explicit (add specific skill names when ready)
protected override IReadOnlyList<string> SkillNames => [];

// Option B — LLM selects (uncomment when resolver is wired up)
// protected override bool AutoSelectSkills => true;
```

Change `BuildMessages` call:
```csharp
// FROM:
var messages = BuildMessages(userContent, contextAgent);

// TO:
var messages = await ResolveAndBuildAsync(userContent, chatClient, ct);
```

Add using:
```csharp
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Ports;
```

---

### `QuestionTriageHandler.cs`

Make it extend `CommandHandlerBase`. It has its own custom message structure (builds a question list for the user message) so it keeps its own `BuildTriageMessages` helper but delegates to `ResolveAndBuildAsync` for the final composition.

Change class declaration:
```csharp
// FROM:
internal sealed class QuestionTriageHandler(IMarkFileReader reader) : ICommandHandler

// TO:
internal sealed class QuestionTriageHandler(
    IMarkFileReader   reader,
    IStepPromptLayer? stepCtx = null)
    : CommandHandlerBase(stepCtx)
    // no resolver — question triage needs no methodology skills
```

Add `SynthesisInstruction` property (replaces the static `SystemPrompt` constant):
```csharp
protected override string SynthesisInstruction =>
    "You are triaging open questions from a prior agent session. " +
    "Based on the iteration context provided, classify each question. " +
    "Respond with valid JSON only.";
```

Remove the old `private const string SystemPrompt` constant.

Rename the private static `BuildMessages` method to `BuildTriageMessages` to avoid shadowing:
```csharp
// FROM: private static IReadOnlyList<ChatMessage> BuildMessages(...)
// TO:   private static IReadOnlyList<ChatMessage> BuildTriageMessages(...)
```

Update its call site inside `ExecuteAiCommandAsync`:
```csharp
// FROM:
var messages = BuildMessages(context.LastOutput() ?? string.Empty, openQuestions);

// TO:
var messages = BuildTriageMessages(context.LastOutput() ?? string.Empty, openQuestions);
```

Inside `BuildTriageMessages`, replace the hardcoded system message using the role context:
```csharp
// The system message in BuildTriageMessages currently uses the static SystemPrompt constant.
// Now that SynthesisInstruction exists on the base class, update BuildTriageMessages to
// call ResolveAndBuildAsync instead of assembling messages manually.
// Simplest approach: compose userContent from the question list and delegate to base:

// In ExecuteAiCommandAsync, replace the BuildTriageMessages call with:
var userContent = BuildTriageUserContent(context.LastOutput() ?? string.Empty, openQuestions);
var messages    = await ResolveAndBuildAsync(userContent, chatClient, ct);
```

Add helper that builds only the user content string:
```csharp
private static string BuildTriageUserContent(
    string iterationContext, IReadOnlyList<Question> openQuestions)
{
    var questionLines = string.Join('\n',
        openQuestions.Select(q => $"  - {q.Id}: {q.Text}"));

    return $"""
        {iterationContext}

        Open questions requiring triage:
        {questionLines}

        For each question:
          - If the session context resolves it → status: "resolved", provide the answer
          - If no longer relevant → status: "obsolete"
          - Otherwise → status: "still_open", classify severity as "hard" or "soft"
        """;
}
```

Add using:
```csharp
using AgentFramework.Core.Agent.Prompts;
```

---

## Step 11 — Update `KickoffStep.cs`

### Add using

```csharp
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Ports;
```

### Add `ISkillResolver` constructor parameter

```csharp
// FROM:
public KickoffStep(
    int stepNumber,
    string instructions,
    Gate gate,
    IMarkFileReader? markFileReader = null)
    : base(stepNumber, "Kickoff-context", instructions, gate)
{
    _markFileReader = markFileReader ?? new NullMarkFileReader();
}

// TO:
private readonly ISkillResolver? _skillResolver;

public KickoffStep(
    int              stepNumber,
    string           instructions,
    Gate             gate,
    IMarkFileReader? markFileReader  = null,
    ISkillResolver?  skillResolver   = null)
    : base(stepNumber, "Kickoff-context", instructions, gate)
{
    _markFileReader  = markFileReader ?? new NullMarkFileReader();
    _skillResolver   = skillResolver;
}
```

### Replace `ExecuteStepAsync` — remove dead code, update chain assembly

```csharp
public override async Task<StepResult?> ExecuteStepAsync(
    IAgentRunContext? context,
    ISessionWriter    writer,
    IChatClient       chatClient,
    CancellationToken ct = default)
{
    var stepCtx = BuildStepContext(context);   // IStepPromptLayer — Role only

    var chain = new KickoffContextChain(
        new CheckpointValidatorCommand(stepCtx),                          // role only, no skills
        new MarkFileLoaderHandler(_markFileReader),                       // deterministic
        new IterationEvaluatorHandler(),                                  // deterministic
        new QuestionTriageHandler(_markFileReader, stepCtx),              // role only, no skills
        new ObjectiveSynthesisHandler(stepCtx, _skillResolver));          // role + dynamic skills

    var (finalJson, journal) = await chain.RunAsync(context, writer, chatClient, ct);

    Journal.Clear();
    Journal.AddRange(journal);

    writer.RecordStepExchange(StepNumber, Name, [], finalJson);

    using var doc = JsonDocument.Parse(finalJson);
    var gateSatisfied = !doc.RootElement.TryGetProperty("gateSatisfied", out var gs)
                        || gs.GetBoolean();
    return ParseResult(doc.RootElement, finalJson, gateSatisfied);
}
```

---

## Step 12 — Update `FileSkillProvider.cs` → `FileSkillResolver.cs`

Rename the file to `FileSkillResolver.cs` and replace its contents:

```csharp
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Infrastructure.Skills;

/// <summary>
/// File-system implementation of ISkillResolver.
/// GetCatalogAsync reads frontmatter only from all SKILL.md files (lightweight).
/// ResolveAsync reads and parses full SKILL.md files for the requested names only.
/// </summary>
public sealed class FileSkillResolver : ISkillResolver
{
    private readonly string _basePath;

    public FileSkillResolver(string basePath)
        => _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));

    public async Task<IReadOnlyList<SkillSummary>> GetCatalogAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_basePath))
            return [];

        var summaries = new List<SkillSummary>();

        foreach (var dir in Directory.GetDirectories(_basePath))
        {
            var skillFile = Path.Combine(dir, "SKILL.md");
            if (!File.Exists(skillFile)) continue;

            var markdown = await File.ReadAllTextAsync(skillFile, ct);
            summaries.Add(SkillParser.ParseSummaryFromMarkdown(markdown));
        }

        return summaries.AsReadOnly();
    }

    public async Task<IReadOnlyList<Skill>> ResolveAsync(
        IReadOnlyList<string> skillNames,
        CancellationToken     ct = default)
    {
        var skills = new List<Skill>();

        foreach (var name in skillNames)
        {
            var path = Path.Combine(_basePath, name, "SKILL.md");
            if (!File.Exists(path)) continue;

            var markdown = await File.ReadAllTextAsync(path, ct);
            skills.Add(SkillParser.ParseFromMarkdown(markdown));
        }

        // Return in the same order as requested — caller controls composition order
        return skills.AsReadOnly();
    }
}
```

Update `ServiceCollectionExtensions.cs` to register `FileSkillResolver` instead of `FileSkillProvider`:

```csharp
// FROM: services.AddSingleton<ISkillProvider, FileSkillProvider>(...)
// TO:   services.AddSingleton<ISkillResolver, FileSkillResolver>(...)
```

---

## Step 13 — Update `UxStepBuilder.cs` and `UxAgent.cs`

Skills are no longer attached at pipeline build time — each command resolves its own skills at execution time. Remove skill attachment from the builder entirely.

### `UxStepBuilder.cs` — replace entire file

```csharp
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

public class UxStepBuilder
{
    private readonly List<AgentStep> _steps = [];

    public static UxStepBuilder Create() => new();

    public UxStepBuilder WithStep(AgentStep step)
    {
        _steps.Add(step);
        return this;
    }

    public UxStepBuilder WithSteps(IEnumerable<AgentStep> steps)
    {
        _steps.AddRange(steps);
        return this;
    }

    public StepPipeline Build()
    {
        if (_steps.Count == 0)
            throw new InvalidOperationException(
                "At least one step is required. Call WithSteps() before Build().");

        return new StepPipeline(_steps);
    }
}
```

### `UxAgent.cs` — replace entire file

```csharp
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

public class UxPersona : AgentAggregate<string>
{
    public UxPersona(
        Role       pRole,
        AgentStep[] pSteps,
        string     projectId,
        SessionMarkFilePaths markFilePaths)
        : base("ux-persona-architect", pRole, projectId, markFilePaths)
    {
        if (pRole is null)
            throw new InvalidOperationException(
                "Role is required. Call WithRole() before Build().");

        if (pSteps.Length == 0)
            throw new InvalidOperationException(
                "At least one step is required. Call WithSteps() before Build().");

        Pipeline = UxStepBuilder.Create()
            .WithSteps(pSteps)
            .Build();
    }
}
```

---

## Step 14 — Build and verify

```bash
cd src
dotnet build AgentFramework.Core/AgentFramework.Core.csproj
dotnet build AgentFramework.Domain/AgentFramework.Domain.csproj
dotnet build AgentFramework.Infrastructure/AgentFramework.Infrastructure.csproj
```

### Expected compile errors and fixes

| Error | Fix |
|---|---|
| `ISkillProvider` not found | Replace all usages with `ISkillResolver` |
| `context.Role` not found | Replace with `context.PromptContext` — Step uses `BuildStepContext(context)` |
| `step.Skill` not found | Removed — skills are per-command, not per-step |
| `step.AttachSkill` / `AttachSkills` not found | Removed — delete call sites |
| `pipelineFactory.CreatePipelineAsync(role, skillProvider, ct)` | Remove `skillProvider` argument |
| `BuildMessages(userContent, contextAgent)` not found | Replace with `await ResolveAndBuildAsync(userContent, chatClient, ct)` |
| `skills` param in `UxPersona` constructor call | Remove from all call sites |

---

## Invariants — Do Not Break

- `PromptContext` is always immutable. Never add mutable fields or non-init setters.
- `ICommandPromptLayer.BuildMessages` is the **only** place that creates `ChatMessage[]`. It is created ephemerally inside `ResolveAndBuildAsync` and never exposed outside.
- `IAgentRunContext` in `ExecuteAiCommandAsync` is for **session data only** (checkpoints, questions, history). Never use it for prompt building.
- `ISkillResolver` is **data only** — no selection logic. Selection belongs to `CommandHandlerBase`.
- The `IChatClient` used for skill selection (Pattern B) is the **same instance** passed to `ExecuteAiCommandAsync` — never stored, never injected separately.
- `GetCatalogAsync` returns name + description only — full instructions are never loaded until `ResolveAsync` is called.
- Skills are returned by `ResolveAsync` in the **same order as requested** — the caller controls composition order.
- Deterministic handlers (`MarkFileLoaderHandler`, `IterationEvaluatorHandler`) do not extend `CommandHandlerBase` and receive no `IStepPromptLayer`.
