using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.UxAgent.Express.Handlers;

/// <summary>
/// Handler 2 of 6 in the UxExpress chain.
/// Synthesises all distilled islands and group decisions into rich HTML buyer persona cards.
/// The LLM produces the complete HTML — no C# template building.
/// </summary>
internal sealed class BuyerPersonaDocumentHandler : CommandHandlerBase
{
    internal const string HtmlSchema = """
        {
          "html": "string — complete, self-contained HTML5 document with embedded CSS and all persona cards"
        }
        """;

    private const string Instruction =
        "Generate an HTML buyer persona document synthesised from the distilled research data. " +
        "Structure: one rich card per persona (one per group) containing:\n" +
        "  • Emoji avatar, archetype name badge (persona name in #e6edf3)\n" +
        "  • Demographics table (age range, location context, role/stage)\n" +
        "  • JTBD statement in verbatim format: \"When [situation], I want to [motivation], so I can [outcome]\"\n" +
        "  • Goals list (3-5 items, in #3fb950)\n" +
        "  • Pains list (3-5 items, in #f85149)\n" +
        "  • Behavioral patterns (3-5 observable habits)\n" +
        "  • Usage scenario narrative (device + location + emotional state + action)\n" +
        "  • Quotable quote in a <blockquote>\n" +
        "  • Anti-persona flag with explanation where the data supports it\n" +
        "Personas must be grounded in captured data only and distinct enough to drive different design decisions. " +
        "Follow the express-as-html skill for output format and visual standards.";

    public BuyerPersonaDocumentHandler(IStepPromptLayer? stepCtx = null)
        : base(stepCtx, Instruction) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext?  agentContext,
        ISessionWriter     writer,
        IChatClient?       chatClient,
        CancellationToken  ct)
    {
        var session      = agentContext?.Session;
        var groups       = agentContext?.Brain?.Groups ?? [];
        var decisions    = agentContext?.Decisions ?? [];
        var deliverables = agentContext?.Deliverables ?? [];
        var objective    = session?.CurrentCheckpoint?.SessionObjective ?? "No objective";
        var userIntent   = session?.CurrentCheckpoint?.UserIntent ?? "No intent recorded";

        // Distilled islands carry the persona research data
        var distilledIslands = agentContext?.Brain?.Backlog?.GetByStatus(IslandStatus.Distilled) ?? [];

        var groupLines = groups.Any()
            ? string.Join("\n", groups.Select(g =>
                $"  [{g.Id}] {g.Name} — Why Together: {g.WhyTogether} (Readiness: {g.Readiness})"))
            : "  No groups";

        var islandLines = distilledIslands.Any()
            ? string.Join("\n", distilledIslands.Select(i =>
                $"  [{i.Id}] [{i.Type}] GroupId:{i.GroupId} — {i.Description}"))
            : "  No distilled islands";

        var decisionLines = decisions.Any()
            ? string.Join("\n", decisions.Select(d =>
                $"  [{d.Id}] GroupId:{d.GroupId} — {d.Description} (Impact: {d.Impact})"))
            : "  No decisions";

        var deliverableLines = deliverables.Any()
            ? string.Join("\n", deliverables.Select(d =>
                $"  [{d.DeliverableId}] GroupId:{d.GroupId} — {d.Purpose} ({d.Path})"))
            : "  No deliverables";

        var userContent = $"""
            Product Context: {userIntent}
            Session Objective: {objective}

            Persona Groups ({groups.Count}):
            {groupLines}

            Distilled Research Islands ({distilledIslands.Count}):
            {islandLines}

            Decisions per Group:
            {decisionLines}

            Deliverables per Group:
            {deliverableLines}

            Synthesise the research above into distinct buyer personas (one per group).
            Every persona must map to a group. Include at least one anti-persona when the data supports it.
            Personas must be differentiated enough to drive different product decisions.

            Return JSON: {HtmlSchema}
            """;

        var messages = BuildPrompt(userContent, skillNames: ["persona-document", "express-as-html"]);
        var json = await chatClient!.SendHandlerAsync(
            messages, HtmlSchema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(userContent, json),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }
}
