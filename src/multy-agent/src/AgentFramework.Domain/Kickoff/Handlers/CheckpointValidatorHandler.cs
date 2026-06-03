using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Domain.Kickoff.Handlers;

internal sealed class CheckpointValidatorCommand : CommandHandlerBase
{
    private const string Schema = """
        {
          "sessionContext": "Summary context of previous sessions goals to be used in the definition of the next goal based on the user request"
        }
        """;

    private const string Instruction =
        """
        You are an assistant helping to Kickoff the context of an agent's previous checkpoints sessions, to define the Goal of the next iteration.
        Review the following list of previous checkpoints and generate a summary context of the previous sessions goals as valid information to be used in the definition of the next goal based on the user request.
        """;
        

    public CheckpointValidatorCommand(IStepPromptLayer? stepCtx = null)
        : base(stepCtx, Instruction) { }

    public override async Task<HandlerExchange> ExecuteAiCommandAsync(
        IReadOnlyList<HandlerExchange> context,
        IAgentRunContext?  contextAgent,
        ISessionWriter     writer,
        IChatClient?       chatClient,
        CancellationToken  ct)
    {
        var lines = new List<string>();

        foreach (var checkpoint in
            contextAgent?.Session?.Checkpoints.OrderByDescending(c => c.SessionIteration)
            ?? Enumerable.Empty<Checkpoint>())
        {
            lines.Add($"Checkpoint #{checkpoint.SessionIteration} " +
                      $"({checkpoint.CreatedAt:yyyy-MM-dd}): " +
                      $"Request: {checkpoint.UserIntent}. " +
                      $"Objective: {checkpoint.SessionObjective}. " +
                      $"Closed: {(checkpoint.ClosedAt.HasValue ? "yes" : "in-progress")}.");
        }

        if (lines.Count == 0)
        {
            return new HandlerExchange(
                GetType().Name,
                new HandlerContent(context.LastOutput() ?? "[start]", "No previous checkpoints available."),
                new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
        }

        var userContent = $"""
            Previous checkpoints:
            {string.Join("\n", lines)}
            """;

        var messages = BuildPrompt(userContent, omitSkills: true);
        var json = await chatClient!.SendHandlerAsync(messages, Schema, root => root.GetRawText(), ct);

        return new HandlerExchange(
            GetType().Name,
            new HandlerContent(context.LastOutput() ?? "[start]", json),
            new HandlerMetadata(context.LastOrDefault()?.Sender, IsLlmCall: true));
    }
}
