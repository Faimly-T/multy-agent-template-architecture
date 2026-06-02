using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.UxAgent.Express.Handlers;

/// <summary>
/// Handler 4 of 6 in the UxExpress chain.
/// Generates a structured HTML interview guide for validating each buyer persona.
/// Every question maps to a testable hypothesis. The LLM produces the HTML — no C# builders.
/// </summary>
internal sealed class PersonaInterviewScriptHandler : CommandHandlerBase
{
    internal const string HtmlSchema = """
        {
          "html": "string — complete, self-contained HTML5 document with embedded CSS and the full interview guide"
        }
        """;

    private const string Instruction =
        "Generate an HTML interview script for validating each buyer persona. " +
        "Structure per persona:\n" +
        "  • Warm-up questions (open, past-tense, rapport-building)\n" +
        "  • Core behavioral questions — NEVER hypothetical, ALWAYS past behavior — " +
        "each with: hypothesis being tested (tag in #388bfd), confirming signal (in #3fb950), " +
        "disconfirming signal (in #f85149), and 3-5 follow-up probes\n" +
        "  • Validation questions (present a persona assumption and ask for the participant's reaction)\n" +
        "  • Closing questions including a referral ask\n" +
        "Global sections: interviewer guidelines at the top of the document, " +
        "scoring rubric table (hypothesis | confirmed-if | disconfirmed-if), " +
        "note-taking template at the bottom. " +
        "Follow the express-as-html skill for output format and visual standards.";

    public PersonaInterviewScriptHandler(IStepPromptLayer? stepCtx = null)
        : base(stepCtx, Instruction) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext?  agentContext,
        ISessionWriter     writer,
        IChatClient?       chatClient,
        CancellationToken  ct)
    {
        var personaOutput  = context.GetOutput(nameof(BuyerPersonaDocumentHandler))   ?? "{}";
        var researchOutput = context.GetOutput(nameof(ResearchValidationGuideHandler)) ?? "{}";

        var session    = agentContext?.Session;
        var objective  = session?.CurrentCheckpoint?.SessionObjective ?? "No objective";
        var userIntent = session?.CurrentCheckpoint?.UserIntent ?? "No intent recorded";

        var userContent = $"""
            Product Context: {userIntent}
            Session Objective: {objective}

            Buyer Persona Document (HTML — extract persona names, goals, pains, assumptions):
            {personaOutput}

            Research Validation Guide (HTML — extract hypotheses to test):
            {researchOutput}

            Design a structured interview script for each persona.
            Rules: warm-up = past-tense open questions; core = behavioral not hypothetical;
            every core question must have hypothesis + confirming + disconfirming signals;
            3-5 follow-up probes per core question; closing must include referral ask.

            Return JSON: {HtmlSchema}
            """;

        var messages = BuildPrompt(userContent, skillNames: ["interview-script", "express-as-html"]);
        var json = await chatClient!.SendHandlerAsync(
            messages, HtmlSchema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(userContent, json),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }
}
