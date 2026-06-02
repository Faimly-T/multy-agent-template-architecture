namespace AgentFramework.Core.Agent.Session;

// ── Capture ──────────────────────────────────────────────────────────────────

public record CapturedIsland(
    string     Id,
    IslandType Type,
    string     Description,
    string     Source,
    string?    RelatesToIslandId = null);

// ── Organize ─────────────────────────────────────────────────────────────────

public record IslandOrganization(string IslandId, IslandStatus NewStatus, string? GroupId = null);

public record IslandGroup(
    string                Id,
    string                Name,
    IReadOnlyList<string> IslandIds,
    string                WhyTogether,
    string                Readiness,
    string?               ReadinessNotes = null);

public record DecisionRecord(string Id, string Description, string Impact);

// ── Distill ───────────────────────────────────────────────────────────────────

public record IslandDistillation(string IslandId, IslandStatus NewStatus);
