namespace AgentFramework.Core.Agent.Session;

public record TokenConsumption(int InputTokens, int OutputTokens)
{
    public int TotalTokens => InputTokens + OutputTokens;
}
