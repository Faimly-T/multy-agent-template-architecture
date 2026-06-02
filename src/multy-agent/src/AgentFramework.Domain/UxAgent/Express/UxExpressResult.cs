using AgentFramework.CodePipeline;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.UxAgent.Express;

/// <summary>
/// The result of the UX Express step.
/// Extends ExpressResult (question review + token consumption) and adds five HTML documents:
///   1. HtmlSummary      — 5-step session journey narrative
///   2. HtmlPersona      — buyer persona cards
///   3. HtmlResearch     — LLM deep-research validation guide
///   4. HtmlInterview    — user interview script
///   5. HtmlNextSession  — reflection questions + next-iteration prompt template
///
/// All five HTML documents are produced directly by the LLM — no C# template building.
/// UxHtmlDeliverableWriter reads these properties and writes them to disk.
/// </summary>
public record UxExpressResult(
    string Output,
    bool   GateSatisfied,
    int    InputTokens,
    int    OutputTokens,
    IReadOnlyList<QuestionRecord> Questions,
    string HtmlSummary,
    string HtmlPersona,
    string HtmlResearch,
    string HtmlInterview,
    string HtmlNextSession) : ExpressResult(Output, GateSatisfied, InputTokens, OutputTokens, Questions)
{
    // ApplyTo is inherited from ExpressResult:
    //   - writer.UpdateTokenConsumption(InputTokens, OutputTokens)
    //   - writer.ReviewQuestion(id, status, text) for every question
    // UxHtmlDeliverableWriter reads the HTML documents and writes them to disk.
    // ServiceBusDeliverableWriter publishes the results to the message bus.
}
