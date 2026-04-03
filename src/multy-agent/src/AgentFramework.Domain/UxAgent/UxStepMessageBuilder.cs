using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Domain.UxAgent;

public class UxStepMessageBuilder : IStepMessageBuilder
{
    public IReadOnlyList<ChatMessage> BuildMessages(AgentStep step, Role role, AgentSession? session, ConversationHistory history)
    {
        var messages = new List<ChatMessage>();

        if (!history.Messages.Any(m => m.Role == MessageRole.System))
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
        var context = BuildStepContext(step, session);
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

    private static string BuildStepContext(AgentStep step, AgentSession? session)
    {
        return step.SkillName switch
        {
            "rehydrate-context" => BuildRehydrateContext(session),
            "autonomous-capture" => BuildCaptureContext(session),
            "strategic-organize" => BuildOrganizeContext(session),
            "expert-distill" => BuildDistillContext(session),
            "express-relay" => BuildRelayContext(session),
            _ => string.Empty
        };
    }

    private static string BuildRehydrateContext(AgentSession? session)
    {
        var objective = session?.Checkpoint.SessionObjective ?? "No objective set yet";
        return $"Current session objective: {objective}";
    }

    private static string BuildCaptureContext(AgentSession? session)
    {
        var objective = session?.Checkpoint.SessionObjective ?? "No objective";
        return $"Session Objective: {objective}";
    }

    private static string BuildOrganizeContext(AgentSession? session)
    {
        if (session is null || session.Islands.Count == 0)
            return "Current Islands: None captured yet";

        var lines = session.Islands.Select(i =>
            $"- {i.Id} [{i.Type}] {i.Description} (Status: {i.Status})" +
            (i.RelatesToIslandId is not null ? $" → relates to {i.RelatesToIslandId}" : ""));
        return $"Current Islands:\n{string.Join("\n", lines)}";
    }

    private static string BuildDistillContext(AgentSession? session)
    {
        if (session is null) return "No session context";

        var organized = session.Islands
            .Where(i => i.Status == IslandStatus.Organized)
            .Select(i => $"- {i.Id} [{i.Type}] {i.Description}");

        var decisions = session.Decisions
            .Select(d => $"- {d.Id}: {d.Description} (Impact: {d.Impact})");

        return $"""
            Organized Islands:
            {string.Join("\n", organized)}

            Decisions so far:
            {(decisions.Any() ? string.Join("\n", decisions) : "None")}
            """;
    }

    private static string BuildRelayContext(AgentSession? session)
    {
        if (session is null) return "No session context";

        var deliverables = session.Deliverables
            .Select(d => $"- {d.DeliverableId}: {d.Path} ({d.Status})");

        var islandStats = $"Islands: {session.Islands.Count} total, " +
            $"{session.Islands.Count(i => i.Status == IslandStatus.Distilled)} distilled, " +
            $"{session.Islands.Count(i => i.Status == IslandStatus.Discarded)} discarded";

        return $"""
            Deliverables produced:
            {(deliverables.Any() ? string.Join("\n", deliverables) : "None")}

            {islandStats}
            Decisions made: {session.Decisions.Count}
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
