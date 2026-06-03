using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.UxAgent.Express.Handlers;

/// <summary>
/// Handler 5 of 6 in the UxExpress chain.
/// Generates an HTML "Next Session Guide" — the bridge document the human researcher
/// uses to reflect on findings and build the next agent iteration request.
///
/// The document contains:
///   1. Session accomplishments summary
///   2. Reflection questions the human should answer before the next session
///   3. Next-iteration checklist (open issues, unvalidated assumptions, follow-on topics)
///   4. A ready-to-paste agent prompt template for starting the second iteration
/// </summary>
internal sealed class NextSessionGuideHandler : CommandHandlerBase
{
    internal const string HtmlSchema = """
        {
          "html": "string — complete, self-contained HTML5 next-session guide with embedded CSS"
        }
        """;

    private const string Instruction =
        "Generate an HTML 'Next Session Guide' that bridges this session to the next agent iteration. " +
        "Structure:\n" +
        "  • Section 1 — What Was Accomplished: 5-7 bullet-point summary of key findings and outputs\n" +
        "  • Section 2 — Reflection Questions (10-15): questions the human MUST answer before the next " +
        "agent session. Cover: persona validation status, market assumptions to verify, product decisions made, " +
        "stakeholder alignment, and open risks. Present as a numbered checklist with checkboxes.\n" +
        "  • Section 3 — Next Iteration Checklist: specific topics, gaps, and follow-on questions " +
        "grouped by: 'Personas to deepen', 'Research to complete', 'Decisions to make', 'Stakeholder inputs needed'\n" +
        "  • Section 4 — Second Iteration Prompt Template: a formatted, copy-paste-ready text block " +
        "pre-populated with session objective, confirmed personas, key open questions, and next-iteration focus. " +
        "Leave [YOUR ANSWER HERE] placeholders for the human to complete.\n" +
        "Use call-to-action button colour #238636 and warning/incomplete sections #b08800. " +
        "Follow the express-as-html skill for output format and visual standards.";

    public NextSessionGuideHandler(IStepPromptLayer? stepCtx = null)
        : base(stepCtx, Instruction) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext?  agentContext,
        ISessionWriter     writer,
        IChatClient?       chatClient,
        CancellationToken  ct)
    {
        // Pull prior documents to ground the reflection questions in actual findings
        var summaryOutput   = context.GetOutput(nameof(UxSessionSummaryHandler))        ?? "{}";
        var personaOutput   = context.GetOutput(nameof(BuyerPersonaDocumentHandler))    ?? "{}";
        var researchOutput  = context.GetOutput(nameof(ResearchValidationGuideHandler)) ?? "{}";
        var interviewOutput = context.GetOutput(nameof(PersonaInterviewScriptHandler))  ?? "{}";

        var session      = agentContext?.Session;
        var objective    = session?.CurrentCheckpoint?.SessionObjective ?? "No objective";
        var userIntent   = session?.CurrentCheckpoint?.UserIntent ?? "No intent recorded";
        var decisions    = agentContext?.Decisions ?? [];
        var questions    = agentContext?.Questions ?? [];
        var deliverables = agentContext?.Deliverables ?? [];

        var openQuestions = questions.Where(q => q.Status == QuestionStatus.Open).ToList();
        var openQuestionsBlock = openQuestions.Any()
            ? string.Join("\n", openQuestions.Select(q => $"  [{q.Id}] {q.Text}"))
            : "  No open questions.";

        var decisionsBlock = decisions.Any()
            ? string.Join("\n", decisions.Select(d => $"  [{d.Id}] {d.Description}"))
            : "  No decisions recorded.";

        var deliverablesBlock = deliverables.Any()
            ? string.Join("\n", deliverables.Select(d => $"  [{d.DeliverableId}] {d.Purpose ?? d.Path}"))
            : "  No deliverables.";

        var userContent = $"""
            User Intent: {userIntent}
            Session Objective: {objective}

            Decisions made this session:
            {decisionsBlock}

            Deliverables produced:
            {deliverablesBlock}

            Open questions (still need answers):
            {openQuestionsBlock}

            Session Summary Document (HTML):
            {summaryOutput}

            Buyer Persona Document (HTML — personas defined this session):
            {personaOutput}

            Research Validation Guide (HTML — what still needs external validation):
            {researchOutput}

            Interview Script (HTML — hypotheses to test with real users):
            {interviewOutput}

            Generate a Next Session Guide that helps the human:
            1. Understand what was accomplished and what still needs validation
            2. Reflect on findings before committing to next steps
            3. Know exactly what questions to bring to the next agent session
            4. Have a ready-to-use prompt template for the second iteration

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
