namespace AgentFramework.Core.Agent.Events;

public record HandlerContent(string Input, string Output);

public record HandlerMetadata(string? RequestedBy, bool IsLlmCall);

public record HandlerExchange(string Sender, HandlerContent Content, HandlerMetadata Metadata)
{
    public static string ProtocolVersion => "1.0";
}

public record HandlerExchanged(int StepNumber, string StepName, HandlerExchange Exchange) : DomainEvent;
