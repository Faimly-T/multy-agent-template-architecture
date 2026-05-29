using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain.Handlers;

internal sealed class ObjectiveSynthesisHandler : CommandHandlerBase
{
    private const string Schema = """
        {
          "sessionObjective": "verb + deliverable + success condition + stakes clause",
          "narrativeBridge": "2-3 sentences connecting prior session to this one",
          "isInitialSession": false,
          "stalenessWarning": "string or null",
          "blockers": [{ "questionId": "Q-001", "text": "...", "severity": "hard|soft" }],
          "gateSatisfied": true
        }
        """;

    private const string Instruction =
        "Synthesize a precise session objective from the context below. " +
        "Respond with valid JSON only.";

    public ObjectiveSynthesisHandler(IStepPromptLayer? stepCtx = null)
        : base(stepCtx, Instruction) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext? contextAgent,
        ISessionWriter writer,
        IChatClient? chatClient,
        CancellationToken ct)
    {

        var checkpointContext = context.GetOutput(nameof(CheckpointValidatorCommand));
        var markFileContext   = context.GetOutput(nameof(MarkFileLoaderHandler));
        var questionContext   = context.GetOutput(nameof(QuestionTriageHandler));

        var userInput = contextAgent?.Session?.UserIntent;
        var userInputLine = string.IsNullOrWhiteSpace(userInput)
            ? string.Empty
            : $"\nUser stated intent: {userInput}\n";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(checkpointContext)) parts.Add(checkpointContext);
        if (!string.IsNullOrWhiteSpace(markFileContext))   parts.Add(markFileContext);
        if (!string.IsNullOrWhiteSpace(questionContext))   parts.Add(questionContext);

        var userContent = $"""
            {string.Join("\n\n", parts)}{userInputLine}

            Synthesize:
              1. sessionObjective — verb + deliverable + success condition + stakes clause
              2. narrativeBridge — 2-3 sentences connecting prior session end to this session start
              3. isInitialSession — true only if no prior session exists
              4. stalenessWarning — include if checkpoint is stale, otherwise null
              5. blockers — list of still-open blockers from prior context, with questionId, text, and severity
              6. gateSatisfied — true if objective is well-formed, false if missing required elements
            """;

        // Skills are pre-loaded into stepCtx.Skills by PipelineCode — BuildPrompt picks them up
        //TODO: here I need to switch and be capable to send the instruction or the name of the skill that I want to unse on this/
        var messages = BuildPrompt(userContent, skillNames: ["Kickoff-context"]);
        var json = await chatClient!.SendHandlerAsync(messages, Schema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(context.LastOutput() ?? "[start]", json),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }
}
