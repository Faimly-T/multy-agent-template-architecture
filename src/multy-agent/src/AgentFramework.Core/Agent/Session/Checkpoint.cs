namespace AgentFramework.Core.Agent.Session;

public record Checkpoint(
    DateTime Date,
    int SessionIteration,
    string SessionObjective,
    TokenConsumption TokensConsumption);
