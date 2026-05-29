using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain.Handlers;

//TODO I need to review and check this selection of skills and how to build the prompt.
internal abstract class CommandHandlerBase(
    IStepPromptLayer? stepCtx              = null,
    string            synthesisInstruction = "",
    ISkillResolver?   resolver             = null) : ICommandHandler
{
    private readonly IStepPromptLayer _stepContext = stepCtx ?? PromptContext.Empty;
    protected readonly string         SynthesisInstruction = synthesisInstruction;
    protected readonly ISkillResolver? Resolver             = resolver;

    protected virtual IReadOnlyList<ChatMessage> BuildPrompt(
        string userContent, 
        bool withOutSkills = false,
        IReadOnlyList<string>? skillNames = null)
    {
        List<Skill>? skillSelected = null;
        if (skillNames != null && skillNames.Count > 0 && _stepContext.Skills != null)
        {
            skillSelected = _stepContext.Skills.Where(s => skillNames.Contains(s.Name)).ToList();
        }

        var resolvedSkills = skillSelected != null || withOutSkills
          ? skillSelected : _stepContext.Skills;
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
