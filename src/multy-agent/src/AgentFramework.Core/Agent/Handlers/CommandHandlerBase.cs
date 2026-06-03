using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Handlers;

/// <summary>
/// Base class for all domain command handlers. Provides prompt construction and skill injection.
///
/// <b>Context-chain threading (Denis Rothman — Context Engineering for Multi-Agent Systems):</b>
/// Each handler in the chain compresses prior reasoning into its prompt by passing the previous
/// handler's output as the user-turn content. This keeps every LLM call focused while
/// maintaining continuity across the chain:
/// <code>
///   Handler 1 → produces output A
///   Handler 2 → receives A as context → produces output B (compressed A + new insight)
///   Handler 3 → receives B as context → produces final output (compressed history + new reasoning)
/// </code>
/// Use <see cref="HandlerContextExtensions.LastOutput"/> or
/// <see cref="HandlerContextExtensions.GetOutput"/> to access the right prior context.
///
/// <b>Skill selection — two paths:</b>
/// <list type="bullet">
///   <item>
///     <b>Hardcoded</b>: pass <c>skillNames</c> to <see cref="BuildPrompt"/> to inject
///     specific skills by name. Used by all current handlers — the skill catalog is small
///     enough that explicit selection is maintainable.
///   </item>
///   <item>
///     <b>LLM-driven</b>: call <see cref="SelectViaLlmAsync"/> and let the model choose
///     the most relevant skills from the catalog. Use when the catalog grows large enough
///     that static selection becomes a maintenance burden.
///   </item>
/// </list>
/// </summary>
public abstract class CommandHandlerBase(
    IStepPromptLayer? stepCtx              = null,
    string            synthesisInstruction = "") : ICommandHandler
{
    private readonly IStepPromptLayer _stepContext = stepCtx ?? PromptContext.Empty;
    protected readonly string SynthesisInstruction = synthesisInstruction;

    /// <summary>
    /// Set by <c>SequentialCommandHandlerChain.RunAsync</c> before each execution when a
    /// repository-backed instruction override is available. <c>null</c> means "use the
    /// compiled-in <see cref="SynthesisInstruction"/>."
    /// </summary>
    internal string? RuntimeInstructionOverride { get; set; }

    private string EffectiveInstruction => RuntimeInstructionOverride ?? SynthesisInstruction;

    /// <summary>
    /// Builds the [System, User] message pair for an LLM call.
    /// </summary>
    /// <param name="userContent">The user-turn content.</param>
    /// <param name="omitSkills">When true the system prompt contains no skill blocks.</param>
    /// <param name="skillNames">
    /// When provided, only skills whose names match this list are injected.
    /// When null and <paramref name="omitSkills"/> is false, all step skills are injected.
    /// </param>
    protected virtual IReadOnlyList<ChatMessage> BuildPrompt(
        string                 userContent,
        bool                   omitSkills = false,
        IReadOnlyList<string>? skillNames = null)
    {
        List<Skill>? skillSelected = null;
        if (skillNames != null && skillNames.Count > 0 && _stepContext.Skills != null)
            skillSelected = _stepContext.Skills.Where(s => skillNames.Contains(s.Name)).ToList();

        var resolvedSkills = skillSelected != null || omitSkills
            ? skillSelected : _stepContext.Skills;

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(_stepContext.RolePrompt))
            parts.Add($"You will act with the following Role:\n{_stepContext.RolePrompt}");

        if (resolvedSkills?.Count > 0)
        {
            var skillsBlock = string.Join("\n\n",
                resolvedSkills.Select(s => $"### Skill: {s.Name}\n{s.Content}"));
            parts.Add($"Available skill frameworks to execute the instruction:\n{skillsBlock}");
        }

        parts.Add($"You will execute the following instruction:\n{EffectiveInstruction}");

        return [
            new ChatMessage(MessageRole.System, string.Join("\n\n", parts)),
            new ChatMessage(MessageRole.User, userContent)
        ];
    }

    /// <summary>
    /// LLM-driven skill selection. Given a skill catalog, asks the model to choose which
    /// skills are most relevant to <see cref="SynthesisInstruction"/>.
    /// Future path: use instead of hardcoded <c>skillNames</c> when the catalog is large.
    /// </summary>
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
