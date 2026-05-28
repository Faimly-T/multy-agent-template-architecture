namespace AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain;

internal static class HandlerContextExtensions
{
    public static string? GetOutput(this IReadOnlyList<HandlerExchange> context, string senderName)
        => context.FirstOrDefault(e => e.Sender == senderName)?.Content.Output;

    public static string? LastOutput(this IReadOnlyList<HandlerExchange> context)
        => context.Count > 0 ? context[^1].Content.Output : null;
}
