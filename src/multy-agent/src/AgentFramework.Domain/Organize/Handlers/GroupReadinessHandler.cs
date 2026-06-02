using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.Organize.Handlers;

/// <summary>
/// Evaluates the readiness of each proposed group for the Distill step using the
/// Golden Circle (WHY/HOW/WHAT) and Rumelt diagnostic lenses.
/// Adds readiness status and notes to each group without modifying the groupings.
/// </summary>
internal sealed class GroupReadinessHandler : CommandHandlerBase
{
    private const string ReadinessSchema = """
        {
          "groups": [
            {
              "id": "GRP-001",
              "name": "string",
              "islandIds": ["ISL-XXX"],
              "whyTogether": "string",
              "readiness": "Ready | OneGap | NeedsCapture | Blocked",
              "readinessNotes": "string describing the gap or null if Ready"
            }
          ]
        }
        """;

    private const string Instruction =
        "You are a UX research strategist evaluating whether each island group has enough material to write a persona card in the Distill phase. " +
        "Lean toward Ready or OneGap — the Distill step is designed to surface gaps as questions, so minor unknowns do not block it. " +
        "Ready: you can identify WHO this person is, WHY they care, and at least one concrete WHAT or pain point — enough to write a persona narrative. " +
        "OneGap: the group is nearly ready but one specific dimension is missing (name it in readinessNotes — e.g. 'HOW direction unclear'). Distill can proceed. " +
        "NeedsCapture: you cannot identify WHO this person is or WHY they care — core identity is missing, not just depth. " +
        "Blocked: an external constraint (e.g. missing legal sign-off, inaccessible stakeholder) prevents distillation. " +
        "Respond with valid JSON only — return the COMPLETE group list with readiness added.";

    public GroupReadinessHandler(IStepPromptLayer? stepCtx = null)
        : base(stepCtx, Instruction) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext? agentContext,
        ISessionWriter    writer,
        IChatClient?      chatClient,
        CancellationToken ct)
    {
        var groupsJson  = context.LastOutput() ?? """{"groups":[]}""";
        var session     = agentContext?.Session;
        var islandLookup = (session?.Backlog.All ?? [])
            .ToDictionary(i => i.Id, i => $"{i.Type}: {i.Description}");

        var userContent = $"""
            Groups to evaluate for readiness:
            {groupsJson}

            Island lookup (for context):
            {string.Join("\n", islandLookup.Select(kv => $"  {kv.Key}: {kv.Value}"))}

            For each group, evaluate:
            Golden Circle:
            - WHY: Do the islands explain why this matters?
            - HOW: Do they suggest how to approach it?
            - WHAT: Do they specify what to build or do?
            Rumelt:
            - Diagnosis: Is the core problem named?
            - Options: Are alternatives present?
            - Guiding Policy: Is a direction emerging?
            - Actions: Are concrete steps implied?

            Return the complete group list with readiness and readinessNotes added.

            Respond using EXACTLY this JSON structure — use these field names verbatim:
            {ReadinessSchema}
            """;

        var messages = BuildPrompt(userContent, skillNames: ["strategic-organize"]);
        var json     = await chatClient!.SendHandlerAsync(messages, ReadinessSchema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(groupsJson, json),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }
}
