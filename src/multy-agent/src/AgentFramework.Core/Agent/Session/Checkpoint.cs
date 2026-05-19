namespace AgentFramework.Core.Agent.Session;

public record Checkpoint(
    DateTime CreatedAt,
    int SessionIteration,
    string SessionObjective,
    TokenConsumption TokensConsumption,
    List<string>? Accomplishments = null);
