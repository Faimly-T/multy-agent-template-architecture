using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.UxAgent.Express.Handlers;

/// <summary>
/// Handler 3 of 6 in the UxExpress chain.
/// Reads persona context from BuyerPersonaDocumentHandler and generates an HTML guide
/// for validating each persona with LLM deep-research tools (Claude, ChatGPT Deep Research, Perplexity).
/// The LLM produces the complete HTML — no C# template building.
/// </summary>
internal sealed class ResearchValidationGuideHandler : CommandHandlerBase
{
    internal const string HtmlSchema = """
        {
          "html": "string — complete, self-contained HTML5 document with embedded CSS and all research guidance"
        }
        """;

    private const string Instruction =
        "Generate an HTML research validation guide for each buyer persona. " +
        "Structure per persona:\n" +
        "  1. A large copyable LLM deep-research prompt (min 100 words, ready to paste into " +
        "Claude/ChatGPT Deep Research/Perplexity — specify what to find, evidence types, " +
        "geographic/time scope, and explicitly request counter-evidence). " +
        "Render prompt boxes with background #1c2128 for visual contrast.\n" +
        "  2. A numbered validation checklist (4-6 specific, falsifiable assumptions to verify)\n" +
        "  3. 'Questions to Answer After Research' list (4-6 questions whose answers " +
        "would change the persona or product decisions)\n" +
        "Additional sections at the bottom: general market-sizing prompt, competitors-to-research list. " +
        "Follow the express-as-html skill for output format and visual standards.";

    public ResearchValidationGuideHandler(IStepPromptLayer? stepCtx = null)
        : base(stepCtx, Instruction) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext?  agentContext,
        ISessionWriter     writer,
        IChatClient?       chatClient,
        CancellationToken  ct)
    {
        // Read persona HTML from the previous handler
        var personaOutput = context.GetOutput(nameof(BuyerPersonaDocumentHandler)) ?? "{}";

        var session    = agentContext?.Session;
        var objective  = session?.CurrentCheckpoint?.SessionObjective ?? "No objective";
        var userIntent = session?.CurrentCheckpoint?.UserIntent ?? "No intent recorded";
        var questions  = agentContext?.Questions ?? [];

        var openQuestions = questions.Where(q => q.Status == QuestionStatus.Open).ToList();
        var openQuestionsContext = openQuestions.Any()
            ? "Open questions that research should answer:\n" +
              string.Join("\n", openQuestions.Select(q => $"  [{q.Id}] {q.Text}"))
            : "No open questions to resolve.";

        var userContent = $"""
            Product Context: {userIntent}
            Session Objective: {objective}

            Buyer Persona Document (HTML — extract persona names, jtbd, goals, pains, and assumptions):
            {personaOutput}

            {openQuestionsContext}

            Design a research validation guide for each persona.
            For each: write a detailed LLM deep-research prompt, list 4-6 falsifiable validation points,
            and list 4-6 post-research questions. Include general market research and competitors sections.

            Return JSON: {HtmlSchema}
            """;

        var messages = BuildPrompt(userContent, skillNames: ["research-validation-guide", "express-as-html"]);
        var json = await chatClient!.SendHandlerAsync(
            messages, HtmlSchema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(userContent, json),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }
}
