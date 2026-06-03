using System.Text;
using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.Capture.Handlers;

/// <summary>
/// Final handler in the Capture chain.
/// Sends the accumulated island list together with open and answered questions to the LLM,
/// asking whether any answered question reveals a user insight not yet captured as an island.
/// Produces the FINAL capture JSON that CaptureStep.ParseResult consumes.
///
/// gateSatisfied = true when the final list contains ≥ 3 islands.
/// </summary>
internal sealed class IslandFromQuestionsHandler : CommandHandlerBase
{
    private const string FinalSchema = """
        {
          "islands": [
            {
              "id": "ISL-XXX",
              "type": "UserType | Goal | PainPoint | BehavioralPattern | ContextOfUse | EmotionalState | AntiUser | Stakeholder | AccessibilitySignal",
              "description": "string",
              "source": "string",
              "relatesToIslandId": "ISL-XXX or null"
            }
          ],
          "gateSatisfied": true
        }
        """;

    private const string Instruction =
        "You are a UX research analyst finalising the island capture phase. " +
        "Review the current island list together with open and answered questions. " +
        "Add a new island ONLY when an answered question reveals a distinct user insight not yet captured. " +
        "Set gateSatisfied to true when the final list contains ≥ 3 islands. " +
        "Respond with valid JSON only — return the COMPLETE final island list.";

    public IslandFromQuestionsHandler(IStepPromptLayer? stepCtx = null)
        : base(stepCtx, Instruction) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext? agentContext,
        ISessionWriter    writer,
        IChatClient?      chatClient,
        CancellationToken ct)
    {
        // Prefer the deliverables handler output; fall back to objective handler if no deliverables ran.
        var currentIslandsJson =
            context.GetOutput(nameof(IslandFromDeliverablesHandler)) ??
            context.GetOutput(nameof(IslandFromObjectiveHandler))    ??
            """{"islands":[]}""";

        var questions     = agentContext?.Questions ?? [];
        var answered      = questions.Where(q => q.Status is QuestionStatus.Answered or QuestionStatus.Reviewed).ToList();
        var open          = questions.Where(q => q.Status == QuestionStatus.Open).ToList();

        // No questions at all → wrap the island list with gateSatisfied and return (no LLM).
        if (answered.Count == 0 && open.Count == 0)
        {
            var noQJson = AppendGateSatisfied(currentIslandsJson);
            return new HandlerExchange(
                GetType().Name,
                new HandlerContent(currentIslandsJson, noQJson),
                new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: false));
        }

        var userContent = BuildUserContent(currentIslandsJson, answered, open);
        var messages    = BuildPrompt(userContent);
        var finalJson   = await chatClient!.SendHandlerAsync(
            messages, FinalSchema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(currentIslandsJson, finalJson),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }

    // --- Content builder ---

    private static string BuildUserContent(
        string currentIslandsJson,
        IReadOnlyList<Question> answered,
        IReadOnlyList<Question> open)
    {
        var answeredSection = answered.Count > 0
            ? "Answered questions — check each for uncaptured user insights:\n" +
              string.Join('\n', answered.Select(q =>
                  $"  [{q.Id}] {q.Text}\n  → Answer: {q.Answer}"))
            : string.Empty;

        var openSection = open.Count > 0
            ? "Still-open questions (for reference — do NOT add islands for these):\n" +
              string.Join('\n', open.Select(q => $"  [{q.Id}] {q.Text}"))
            : string.Empty;

        return $"""
            Current island list:
            {currentIslandsJson}

            {answeredSection}

            {openSection}

            Instructions:
            - Add a new island ONLY when an answered question reveals a user type, goal, pain point,
              or behavior NOT already present in the list. Use the next available ISL-NNN ID.
            - If nothing new is surfaced, return the list unchanged.
            - Set gateSatisfied to true when the final list contains ≥ 3 islands.
            - Return the COMPLETE final island list including gateSatisfied.

            Respond using EXACTLY this JSON structure — use these field names verbatim:
            {FinalSchema}
            """;
    }

    // --- Helpers ---

    /// <summary>
    /// Parses the island JSON from a previous handler and appends gateSatisfied without an LLM call.
    /// </summary>
    private static string AppendGateSatisfied(string islandsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(islandsJson);
            var count = doc.RootElement.TryGetProperty("islands", out var arr) ? arr.GetArrayLength() : 0;

            using var ms     = new MemoryStream();
            using var writer = new Utf8JsonWriter(ms);

            writer.WriteStartObject();
            writer.WritePropertyName("islands");
            (doc.RootElement.TryGetProperty("islands", out var islandsArr) ? islandsArr : default)
                .WriteTo(writer);
            writer.WriteBoolean("gateSatisfied", count >= 3);
            writer.WriteEndObject();
            writer.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }
        catch
        {
            return """{"islands":[],"gateSatisfied":false}""";
        }
    }
}
