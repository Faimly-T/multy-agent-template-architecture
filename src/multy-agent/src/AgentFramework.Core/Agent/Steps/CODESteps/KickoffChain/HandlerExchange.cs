namespace AgentFramework.Core.Agent.Steps.CODESteps.KickoffChain;

public record HandlerContent(string Input, string Output);

public record HandlerMetadata(string? RequestedBy, bool IsLlmCall);

public record HandlerExchange(string Sender, HandlerContent Content, HandlerMetadata Metadata);
