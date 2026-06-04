using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.Distill.Handlers;

/// <summary>
/// Single-call Distill handler. Processes all organized groups in one LLM call.
/// For each group produces: decisions (the HOW), deliverables (with purpose), questions (doubts + feedback requests).
/// Also marks all organized islands as Distilled.
/// Output is the complete DistillStep JSON.
/// </summary>
internal sealed class GroupDistillationHandler : CommandHandlerBase
{
    // Extends BrainAggregate.DistillationSchema with UX-specific question capture.
    // decisions/deliverables/distilledIslands must stay in sync with Brain's parser; questions are handler-specific.
    private const string DistillSchema = """
        {
          "groupDistillations": [
            {
              "groupId": "GRP-001",
              "decisions": [
                { "id": "DEC-001", "description": "string — the HOW decision for this group", "impact": "string", "islandId": "ISL-XXX" }
              ],
              "deliverables": [
                {
                  "deliverableId": "DEL-001",
                  "path": "outputs/personas/filename.html",
                  "purpose": "string — why this deliverable and what it must express",
                  "status": "Draft",
                  "islandIds": ["ISL-001", "ISL-003"],
                  "decisionIds": ["DEC-001"]
                }
              ],
              "questions": [
                {
                  "id": "UX-Q001",
                  "text": "string — clear, open-ended question",
                  "questionType": "Doubt | FeedbackRequest | IdeaValidation",
                  "targetRoles": ["PjM", "Engineering", "Design", "Legal", "Marketing"]
                }
              ]
            }
          ],
          "distilledIslands": [
            { "islandId": "ISL-XXX", "newStatus": "Distilled | Discarded" }
          ],
          "gateSatisfied": true
        }
        """;

    private const string Instruction =
        "You are a strategic analyst performing the Distill phase of a design sprint. " +
        "For each island group: (1) define the HOW decisions — the concrete choices about what to build and how; " +
        "(2) identify the deliverable(s) with a clear purpose statement; " +
        "(3) raise open questions for doubts, feedback requests, and idea validation — include target roles. " +
        "All organized islands become Distilled. " +
        "Respond with valid JSON only.";

    public GroupDistillationHandler(IStepPromptLayer? stepCtx = null)
        : base(stepCtx, Instruction) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext? agentContext,
        ISessionWriter    writer,
        IChatClient?      chatClient,
        CancellationToken ct)
    {
        var brain     = agentContext?.Brain;
        var objective = agentContext?.Session?.CurrentCheckpoint?.SessionObjective ?? "No objective defined";
        var groups    = brain?.Groups ?? [];
        var organized = brain?.Backlog.GetByStatus(IslandStatus.Organized) ?? [];
        var decisions = agentContext?.Decisions ?? [];
        var questions = agentContext?.Questions ?? [];

        var groupContext = BuildGroupContext(groups, organized);
        var questionsContext = questions.Count > 0
            ? $"\nExisting open questions (do not duplicate):\n{string.Join("\n", questions.Select(q => $"  [{q.Id}] {q.Text}"))}"
            : string.Empty;

        var userContent = $"""
            Session Objective: {objective}

            {groupContext}

            Existing decisions:
            {(decisions.Count > 0 ? string.Join("\n", decisions.Select(d => $"  [{d.Id}] {d.Description}")) : "  None")}
            {questionsContext}

            For each group above:
            1. DECISIONS — the HOW: what specifically will be built, designed, or defined to address this group.
               Each decision should reflect Rumelt's Guiding Policy (the logic connecting problem to solution).
            2. DELIVERABLES — one or more concrete outputs. For each, state the PURPOSE:
               what this deliverable expresses and how it satisfies the group's goal.
            3. QUESTIONS — open questions that need answers before or after delivery:
               - Doubts: unresolved uncertainties blocking a decision.
               - FeedbackRequests: ask stakeholders to validate a direction.
               - IdeaValidation: confirm an assumption or hypothesis.
               For each question, specify which roles should review it.

            Mark ALL organized islands as Distilled.
            Use DEC-NNN, DEL-NNN, UX-QNNN for IDs (continue from existing if any).
            gateSatisfied = true when all groups have at least one decision.

            Respond using EXACTLY this JSON structure — use these field names verbatim:
            {DistillSchema}
            """;

        var messages = BuildPrompt(userContent, skillNames: ["expert-distill"]);
        var json     = await chatClient!.SendHandlerAsync(messages, DistillSchema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(groupContext, json),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }

    private static string BuildGroupContext(
        IReadOnlyList<IslandGroup> groups,
        IReadOnlyList<Island> organized)
    {
        var lookup = organized.GroupBy(i => i.GroupId).ToDictionary(g => g.Key ?? string.Empty, g => g.ToList());

        var lines = groups.Select(grp =>
        {
            var islands = lookup.TryGetValue(grp.Id, out var isl)
                ? string.Join("\n", isl.Select(i => $"    - {i.Id} [{i.Type}]: {i.Description}"))
                : "    (no islands assigned)";
            return $"""
                Group [{grp.Id}] {grp.Name} (Readiness: {grp.Readiness})
                  WHY: {grp.WhyTogether}
                  Islands:
                {islands}
                """;
        });

        return string.Join("\n\n", lines);
    }
}
