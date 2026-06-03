using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.UxAgent.Express.Handlers;

/// <summary>
/// Handler 1 of 6 in the UxExpress chain.
/// Generates a complete styled HTML document summarising the full 5-step research session.
/// The LLM produces the HTML directly — no C# template building.
/// </summary>
internal sealed class UxSessionSummaryHandler : CommandHandlerBase
{
    internal const string HtmlSchema = """
        {
          "html": "string — complete, self-contained HTML5 document with embedded CSS and all session data"
        }
        """;

    private const string Instruction =
        "Generate an HTML session summary document covering the full 5-step UX research pipeline. " +
        "Structure:\n" +
        "  • Branded header with project name and session objective\n" +
        "  • 5-step journey timeline (Kickoff → Capture → Organize → Distill → Express) — " +
        "each card shows step number, name, status badge, and 2-4 bullet-point findings\n" +
        "  • Stat cards: total islands, groups formed, decisions made, deliverables produced, open questions\n" +
        "  • Decisions list with impact statement per decision\n" +
        "  • Deliverables list with file paths and completion status\n" +
        "  • Open questions list\n" +
        "  • 'Next Steps' call-to-action section\n" +
        "Use all session data provided. Follow the express-as-html skill for output format and visual standards.";

    public UxSessionSummaryHandler(IStepPromptLayer? stepCtx = null)
        : base(stepCtx, Instruction) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext?  agentContext,
        ISessionWriter     writer,
        IChatClient?       chatClient,
        CancellationToken  ct)
    {
        var session      = agentContext?.Session;
        var backlog      = session?.Backlog;
        var groups       = session?.Groups ?? [];
        var decisions    = agentContext?.Decisions ?? [];
        var deliverables = agentContext?.Deliverables ?? [];
        var questions    = agentContext?.Questions ?? [];
        var objective    = session?.CurrentCheckpoint?.SessionObjective ?? "No objective defined";
        var userIntent   = session?.CurrentCheckpoint?.UserIntent ?? "No intent recorded";

        var islandStats = backlog is not null
            ? $"Total: {backlog.All.Count}, " +
              $"Captured: {backlog.GetByStatus(IslandStatus.Captured).Count}, " +
              $"Organized: {backlog.GetByStatus(IslandStatus.Organized).Count}, " +
              $"Distilled: {backlog.GetByStatus(IslandStatus.Distilled).Count}, " +
              $"Discarded: {backlog.GetByStatus(IslandStatus.Discarded).Count}"
            : "No islands captured";

        var groupLines = groups.Any()
            ? string.Join("\n", groups.Select(g => $"  - [{g.Id}] {g.Name} (Readiness: {g.Readiness})"))
            : "  No groups formed";

        var decisionLines = decisions.Any()
            ? string.Join("\n", decisions.Select(d => $"  - [{d.Id}] {d.Description}"))
            : "  No decisions";

        var deliverableLines = deliverables.Any()
            ? string.Join("\n", deliverables.Select(d => $"  - [{d.DeliverableId}] {d.Path} ({d.Status})"))
            : "  No deliverables";

        var questionLines = questions.Any()
            ? string.Join("\n", questions.Select(q =>
                $"  - [{q.Id}] [{q.Status}] {q.Text}" +
                (q.Answer is not null ? $" → {q.Answer}" : string.Empty)))
            : "  No questions";

        var userContent = $"""
            User Intent: {userIntent}
            Session Objective: {objective}

            Island Statistics:
            {islandStats}

            Groups ({groups.Count}):
            {groupLines}

            Decisions ({decisions.Count}):
            {decisionLines}

            Deliverables ({deliverables.Count}):
            {deliverableLines}

            Questions ({questions.Count}):
            {questionLines}

            Generate a complete, professional HTML session summary covering all 5 steps
            (Kickoff, Capture, Organize, Distill, Express). Use all the data above.

            Return JSON: {HtmlSchema}
            """;

        var messages = BuildPrompt(userContent, skillNames: ["express-as-html"]);
        var json = await chatClient!.SendHandlerAsync(
            messages, HtmlSchema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(userContent, json),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }
}
