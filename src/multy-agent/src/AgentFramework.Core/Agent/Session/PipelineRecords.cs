namespace AgentFramework.Core.Agent.Session;

// Brain-related records (CapturedIsland, IslandOrganization, IslandGroup,
// DecisionRecord, IslandDistillation) live in AgentFramework.Core/Agent/Brain/BrainRecords.cs.
// This file contains only the Distill output records for deliverables and questions.

// ── Deliverables ──────────────────────────────────────────────────────────────

public record DeliverableRecord(string DeliverableId, string Path, DeliverableStatus Status);

public record GroupDeliverableRecord(
    string            GroupId,
    string            DeliverableId,
    string            Path,
    string            Purpose,
    DeliverableStatus Status);

// ── Group Distill output ──────────────────────────────────────────────────────

public record GroupQuestionRecord(
    string                GroupId,
    string                Id,
    string                Text,
    string                QuestionType,
    IReadOnlyList<string> TargetRoles);

public record GroupDistillationRecord(
    string                               GroupId,
    IReadOnlyList<DecisionRecord>        Decisions,
    IReadOnlyList<GroupDeliverableRecord> Deliverables,
    IReadOnlyList<GroupQuestionRecord>   Questions);
