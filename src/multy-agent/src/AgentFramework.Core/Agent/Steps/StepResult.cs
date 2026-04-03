using AgentFramework.Core.Agent.Session;

namespace AgentFramework.Core.Agent.Steps;

public record StepResult(string Output, bool GateSatisfied);

public record RehydrateResult(
    string Output,
    bool GateSatisfied,
    string SessionObjective) : StepResult(Output, GateSatisfied);

public record CaptureResult(
    string Output,
    bool GateSatisfied,
    IReadOnlyList<CapturedIsland> Islands) : StepResult(Output, GateSatisfied);

public record CapturedIsland(
    string Id,
    IslandType Type,
    string Description,
    string Source,
    string? RelatesToIslandId = null);

public record OrganizeResult(
    string Output,
    bool GateSatisfied,
    IReadOnlyList<IslandOrganization> OrganizedIslands,
    IReadOnlyList<DecisionRecord> Decisions) : StepResult(Output, GateSatisfied);

public record IslandOrganization(string IslandId, IslandStatus NewStatus);

public record DecisionRecord(string Id, string Description, string Impact);

public record DistillResult(
    string Output,
    bool GateSatisfied,
    IReadOnlyList<IslandDistillation> DistilledIslands,
    IReadOnlyList<DeliverableRecord> Deliverables) : StepResult(Output, GateSatisfied);

public record IslandDistillation(string IslandId, IslandStatus NewStatus);

public record DeliverableRecord(string DeliverableId, string Path, DeliverableStatus Status);

public record RelayResult(
    string Output,
    bool GateSatisfied,
    int InputTokens,
    int OutputTokens) : StepResult(Output, GateSatisfied);