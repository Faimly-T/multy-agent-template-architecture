using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

public class UxStepMessageBuilder : IStepMessageBuilder
{
    public IReadOnlyList<ChatMessage> BuildMessages(AgentStep step, Role role, AgentSession? session, IReadOnlyList<ChatMessage> conversationHistory)
    {
        var messages = new List<ChatMessage>();

        if (!conversationHistory.Any(m => m.Role == MessageRole.System))
        {
            messages.Add(new ChatMessage(MessageRole.System, BuildSystemPrompt(role)));
        }

        messages.Add(new ChatMessage(MessageRole.User, BuildUserPrompt(step, session)));

        return messages;
    }

    private static string BuildSystemPrompt(Role role)
    {
        return $"""
            You are {role.Identity.Persona}.
            Role: {role.Identity.Role}
            Authority: {role.Identity.Authority}
            Boundary: {role.Identity.Boundary}

            Mandate: {role.Mandate}

            Directives:
            {string.Join("\n", role.FactsAndDirectives.Select(d => $"- {d}"))}

            IMPORTANT: You MUST respond with valid JSON matching the schema provided in each step.
            Do NOT include markdown fences or any text outside the JSON object.
            """;
    }

    private static string BuildUserPrompt(AgentStep step, AgentSession? session)
    {
        var context = step.BuildContext(session);
        var skillBlock = BuildSkillBlock(step);
        var jsonBlock = BuildJsonBlock(step);

        return $"""
            ## Step {step.StepNumber}: {step.Name}

            {context}

            {skillBlock}

            {step.Instructions}

            {jsonBlock}
            """;
    }

    private static string BuildSkillBlock(AgentStep step)
    {
        if (step.Skill is null)
            return string.Empty;

        return $"""
            ### Skill: {step.Skill.Name}
            {step.Skill.Description}

            Follow these instructions:
            {step.Skill.Instructions}
            """;
    }

    private static string BuildJsonBlock(AgentStep step)
    {
        return $"""
            ### Required Response Format
            Respond ONLY with a JSON object matching this schema:
            ```json
            {step.JsonResponseSchema}
            ```
            Gate: {step.Gate.Description}
            """;
    }
}
