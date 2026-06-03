using System.Text.Json;
using AgentFramework.CodePipeline;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Domain.UxAgent.Express.Handlers;

namespace AgentFramework.Domain.UxAgent.Express;

/// <summary>
/// UX-specific Express step. Extends <see cref="ExpressStep"/> to:
/// <list type="bullet">
///   <item>Evaluate gate based on whether the HTML documents were produced (not just a JSON flag).</item>
///   <item>Extract five HTML documents from the chain journal and return a <see cref="UxExpressResult"/>.</item>
/// </list>
/// All chain execution, journal recording, and token/question parsing are inherited from
/// <see cref="ExpressStep"/> — nothing is duplicated here.
/// </summary>
public class UxExpressStep : ExpressStep
{
    public UxExpressStep(
        IStepPromptLayer       stepContext,
        int                    stepNumber,
        string                 instructions,
        Gate                   gate,
        IStepChain?            chain             = null,
        Func<string, string?>? instructionLookup = null)
        : base(stepContext, stepNumber, instructions, gate, chain, instructionLookup) { }

    // ──────────────────────────────────────────────────────────────────────
    // Gate: require at least the two primary HTML documents to be produced
    // ──────────────────────────────────────────────────────────────────────

    protected override bool EvaluateGate(JsonElement root, IReadOnlyList<HandlerExchange> journal)
        => !string.IsNullOrEmpty(ExtractHtml(journal, nameof(UxSessionSummaryHandler)))
        && !string.IsNullOrEmpty(ExtractHtml(journal, nameof(BuyerPersonaDocumentHandler)));

    // ──────────────────────────────────────────────────────────────────────
    // Result: parse base tokens/questions then add five HTML artifacts
    // ──────────────────────────────────────────────────────────────────────

    protected override StepResult BuildChainResult(
        JsonElement root, string rawOutput, bool gateSatisfied,
        IReadOnlyList<HandlerExchange> journal)
    {
        var parsed = (ExpressResult)ParseResult(root, rawOutput, gateSatisfied);

        return new UxExpressResult(
            rawOutput, gateSatisfied,
            parsed.InputTokens, parsed.OutputTokens, parsed.Questions,
            HtmlSummary:     ExtractHtml(journal, nameof(UxSessionSummaryHandler)),
            HtmlPersona:     ExtractHtml(journal, nameof(BuyerPersonaDocumentHandler)),
            HtmlResearch:    ExtractHtml(journal, nameof(ResearchValidationGuideHandler)),
            HtmlInterview:   ExtractHtml(journal, nameof(PersonaInterviewScriptHandler)),
            HtmlNextSession: ExtractHtml(journal, nameof(NextSessionGuideHandler)));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helper — extract "html" field from a named handler's journal entry
    // ──────────────────────────────────────────────────────────────────────

    private static string ExtractHtml(IReadOnlyList<HandlerExchange> journal, string senderName)
    {
        var output = journal.GetOutput(senderName) ?? "{}";
        try
        {
            using var doc = JsonDocument.Parse(output);
            return doc.RootElement.TryGetProperty("html", out var h)
                ? h.GetString() ?? string.Empty
                : string.Empty;
        }
        catch { return string.Empty; }
    }
}
