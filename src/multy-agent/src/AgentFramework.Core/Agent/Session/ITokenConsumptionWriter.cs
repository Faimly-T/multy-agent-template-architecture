namespace AgentFramework.Core.Agent.Session;

public interface ITokenConsumptionWriter
{
    void UpdateTokenConsumption(int inputTokens, int outputTokens);
}
