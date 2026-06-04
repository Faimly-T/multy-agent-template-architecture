using System.Text.Json;

namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// DDD Aggregate Root for the project's knowledge base.
///
/// One Brain exists per project and is shared by all agents working on that project.
/// It accumulates islands, groups, decisions, and deliverables across every checkpoint
/// and every agent run — it is never reset.
///
/// <b>Two ways to mutate the Brain:</b>
/// <list type="bullet">
///   <item>
///     <b>JSON (LLM-first path)</b> — handlers call <see cref="AddIslands"/>,
///     <see cref="ApplyOrganizationJson"/>, or <see cref="ApplyDistillationJson"/> with
///     raw LLM output. The Brain owns schema validation and parsing:
///     <code>
///     brain.AddIslands(await chatClient.SendHandlerAsync(
///         messages, BrainAggregate.IslandSchema, r => r.GetRawText(), ct));
///     </code>
///   </item>
///   <item>
///     <b>Typed (pipeline/test path)</b> — <see cref="IBrainWriter"/> methods
///     (<see cref="SetCapturedIslands"/>, <see cref="ApplyOrganization"/>,
///     <see cref="ApplyDistillation"/>) are called by <see cref="AgentAggregate{TId}"/>
///     via <c>StepResult.ApplyTo</c>. These remain for backward compatibility and testing.
///   </item>
/// </list>
///
/// <b>Accumulation semantics:</b>
/// <list type="bullet">
///   <item>Islands — merge by ID (new islands added, existing IDs skipped).</item>
///   <item>Groups  — upsert by ID (add new, update existing content, never delete).</item>
///   <item>Decisions + Deliverables — append only (skip duplicate IDs).</item>
/// </list>
///
/// <b>Checkpoint audit:</b>
/// Call <see cref="BeginCheckpoint"/> at the start of an agent run and
/// <see cref="FinalizeCheckpoint"/> at the end. The Brain records what each agent
/// added in each checkpoint so you can answer "what did ux-persona add in session 2?"
/// </summary>
public sealed class BrainAggregate
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public BrainId Id { get; }

    // ── Reasoning State ───────────────────────────────────────────────────────
    private readonly IslandBacklog     _backlog      = new();
    private readonly List<IslandGroup> _groups       = [];
    private readonly List<Decision>    _decisions    = [];
    private readonly List<Deliverable> _deliverables = [];

    // ── Audit Trail ───────────────────────────────────────────────────────────
    private readonly List<BrainCheckpoint> _checkpoints     = [];
    private          CheckpointBuilder?    _activeCheckpoint;

    // ── Construction ──────────────────────────────────────────────────────────

    public BrainAggregate(BrainId id) { Id = id; }

    /// <summary>Creates a fresh Brain for a project. Use <see cref="FromState"/> to restore a persisted Brain.</summary>
    public BrainAggregate(string projectId) : this(new BrainId(projectId)) { }

    // ── Read Access ───────────────────────────────────────────────────────────

    /// <summary>All captured islands with their current status in the reasoning pipeline.</summary>
    public IslandBacklog Backlog => _backlog;

    /// <summary>All islands — convenience shortcut for <c>Backlog.All</c>.</summary>
    public IReadOnlyList<Island> Islands => _backlog.All;

    /// <summary>Semantic groups formed during the Organize phase — accumulates across checkpoints.</summary>
    public IReadOnlyList<IslandGroup> Groups => _groups.AsReadOnly();

    /// <summary>All decisions made during Organize and Distill phases — append only.</summary>
    public IReadOnlyList<Decision> Decisions => _decisions.AsReadOnly();

    /// <summary>All deliverables planned or produced during Distill and Express phases — append only.</summary>
    public IReadOnlyList<Deliverable> Deliverables => _deliverables.AsReadOnly();

    /// <summary>Audit trail: one entry per agent checkpoint that mutated this Brain.</summary>
    public IReadOnlyList<BrainCheckpoint> Checkpoints => _checkpoints.AsReadOnly();

    // ── Checkpoint Lifecycle ──────────────────────────────────────────────────

    /// <summary>
    /// Opens a new audit checkpoint for the given agent run.
    /// All mutations until <see cref="FinalizeCheckpoint"/> are attributed to this checkpoint.
    /// Called by <see cref="AgentAggregate{TId}"/> at the start of each iteration.
    /// </summary>
    internal void BeginCheckpoint(string agentId, string sessionObjective)
    {
        _activeCheckpoint = new CheckpointBuilder(_checkpoints.Count + 1, agentId, sessionObjective);
    }

    /// <summary>Seals the active checkpoint. Called by <see cref="AgentAggregate{TId}"/> at session end.</summary>
    internal void FinalizeCheckpoint(DateTime closedAt)
    {
        if (_activeCheckpoint is null) return;
        _checkpoints.Add(_activeCheckpoint.Build(closedAt));
        _activeCheckpoint = null;
    }

    // ── LLM Schema Contract ───────────────────────────────────────────────────

    /// <summary>
    /// JSON schema the LLM must follow when returning captured islands (Capture phase).
    /// Reference this in handler prompts so the format never drifts from what Brain can parse.
    /// </summary>
    public static string IslandSchema { get; } = """
        {
          "islands": [
            {
              "id": "ISL-001",
              "type": "UserType | Goal | PainPoint | BehavioralPattern | ContextOfUse | EmotionalState | AntiUser | Stakeholder | AccessibilitySignal",
              "description": "string — one clear insight",
              "source": "string — where this came from (e.g. six-hats:white)",
              "relatesToIslandId": "ISL-XXX or null"
            }
          ]
        }
        """;

    /// <summary>
    /// JSON schema the LLM must follow when returning grouped islands (Organize phase).
    /// </summary>
    public static string GroupSchema { get; } = """
        {
          "groups": [
            {
              "id": "GRP-001",
              "name": "string — sharp name capturing what the islands say",
              "islandIds": ["ISL-001"],
              "whyTogether": "string — one sentence: the shared intent behind this group",
              "readiness": "READY | NEEDS_REVIEW | BLOCKED"
            }
          ],
          "ungroupedIslandIds": ["ISL-XXX"]
        }
        """;

    /// <summary>
    /// JSON schema the LLM must follow when returning distillation output (Distill phase).
    /// Includes decisions (the HOW), deliverables (the OUTPUT), and island status updates.
    /// </summary>
    public static string DistillationSchema { get; } = """
        {
          "groupDistillations": [
            {
              "groupId": "GRP-001",
              "decisions": [
                {
                  "id": "DEC-001",
                  "description": "string — the HOW decision for this group",
                  "impact": "string — what this decision affects",
                  "islandId": "ISL-XXX"
                }
              ],
              "deliverables": [
                {
                  "deliverableId": "DEL-001",
                  "path": "outputs/personas/filename.html",
                  "purpose": "string — why this deliverable exists and what it must express",
                  "status": "Draft",
                  "islandIds": ["ISL-001", "ISL-003"],
                  "decisionIds": ["DEC-001"]
                }
              ]
            }
          ],
          "distilledIslands": [
            { "islandId": "ISL-XXX", "newStatus": "Distilled | Discarded" }
          ]
        }
        """;

    // ── JSON Parse-and-Apply (LLM-first path) ─────────────────────────────────

    /// <summary>
    /// Parses Capture-phase LLM JSON and merges the islands into the Brain.
    /// Handlers call this after receiving the LLM response — Brain owns parsing and accumulation.
    /// </summary>
    public void AddIslands(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var islands = new List<CapturedIsland>();

        foreach (var item in doc.RootElement.GetProperty("islands").EnumerateArray())
        {
            var typeStr = item.GetProperty("type").GetString() ?? "Goal";
            var type    = Enum.TryParse<IslandType>(typeStr, ignoreCase: true, out var parsed) ? parsed : IslandType.Goal;
            var rel     = item.TryGetProperty("relatesToIslandId", out var relProp) && relProp.ValueKind != JsonValueKind.Null
                ? relProp.GetString()
                : null;

            islands.Add(new CapturedIsland(
                item.GetProperty("id").GetString()!,
                type,
                item.GetProperty("description").GetString()!,
                item.GetProperty("source").GetString()!,
                rel));
        }

        SetCapturedIslands(islands);
    }

    /// <summary>
    /// Parses Organize-phase LLM JSON and upserts groups + updates island statuses in the Brain.
    /// </summary>
    public void ApplyOrganizationJson(string json)
    {
        using var doc  = JsonDocument.Parse(json);
        var root       = doc.RootElement;
        var groups     = new List<IslandGroup>();

        if (root.TryGetProperty("groups", out var groupsEl))
            foreach (var g in groupsEl.EnumerateArray())
            {
                var islandIds = g.GetProperty("islandIds").EnumerateArray()
                    .Select(e => e.GetString()!).ToList().AsReadOnly();
                groups.Add(new IslandGroup(
                    g.GetProperty("id").GetString()!,
                    g.GetProperty("name").GetString()!,
                    islandIds,
                    g.GetProperty("whyTogether").GetString()!,
                    g.TryGetProperty("readiness", out var r) ? r.GetString() ?? "READY" : "READY"));
            }

        var organizations = groups
            .SelectMany(g => g.IslandIds.Select(id => new IslandOrganization(id, IslandStatus.Organized, g.Id)))
            .ToList();

        ApplyOrganization(organizations, [], groups);
    }

    /// <summary>
    /// Parses Distill-phase LLM JSON and appends decisions + tracks deliverables in the Brain.
    /// </summary>
    public void ApplyDistillationJson(string json)
    {
        using var doc        = JsonDocument.Parse(json);
        var root             = doc.RootElement;
        var groupDistillations = new List<GroupDistillationRecord>();

        if (root.TryGetProperty("groupDistillations", out var gdEl))
            foreach (var gd in gdEl.EnumerateArray())
            {
                var groupId   = gd.GetProperty("groupId").GetString()!;
                var decisions = ParseDecisionRecords(gd);
                var deliverables = ParseGroupDeliverableRecords(gd, groupId);
                groupDistillations.Add(new GroupDistillationRecord(groupId, decisions, deliverables, []));
            }

        var distillations = new List<IslandDistillation>();
        if (root.TryGetProperty("distilledIslands", out var diEl))
            foreach (var di in diEl.EnumerateArray())
            {
                var statusStr = di.GetProperty("newStatus").GetString() ?? "Distilled";
                var status    = Enum.TryParse<IslandStatus>(statusStr, ignoreCase: true, out var s) ? s : IslandStatus.Distilled;
                distillations.Add(new IslandDistillation(di.GetProperty("islandId").GetString()!, status));
            }

        ApplyDistillation(distillations, groupDistillations);
        TrackDeliverables([], groupDistillations);
    }

    // ── Typed Mutations (IBrainWriter path — called by AgentAggregate) ─────────

    /// <summary>Capture phase — merges new islands into the backlog (accumulates, does not replace).</summary>
    internal void SetCapturedIslands(IReadOnlyList<CapturedIsland> islands)
    {
        var added = _backlog.MergeCaptured(islands);
        _activeCheckpoint?.IslandIdsAdded.AddRange(added);
    }

    /// <summary>Organize phase — upserts groups and updates island statuses.</summary>
    internal void ApplyOrganization(
        IReadOnlyList<IslandOrganization> organizations,
        IReadOnlyList<DecisionRecord>     decisions,
        IReadOnlyList<IslandGroup>        groups)
    {
        if (organizations.Count > 0)
            _backlog.ApplyOrganization(organizations);

        var addedGroupIds    = new List<string>();
        var addedDecisionIds = new List<string>();

        foreach (var g in groups)
        {
            var idx = _groups.FindIndex(x => x.Id == g.Id);
            if (idx >= 0)
                _groups[idx] = g;
            else
            {
                _groups.Add(g);
                addedGroupIds.Add(g.Id);
            }
        }

        foreach (var d in decisions)
            if (_decisions.All(x => x.Id != d.Id))
            {
                _decisions.Add(new Decision(d.Id, d.Description, d.Impact));
                addedDecisionIds.Add(d.Id);
            }

        _activeCheckpoint?.GroupIdsAdded.AddRange(addedGroupIds);
        _activeCheckpoint?.DecisionIdsAdded.AddRange(addedDecisionIds);
    }

    /// <summary>Distill phase — appends decisions and advances island statuses.</summary>
    internal void ApplyDistillation(
        IReadOnlyList<IslandDistillation>       distillations,
        IReadOnlyList<GroupDistillationRecord>  groupDistillations)
    {
        if (distillations.Count > 0)
            _backlog.ApplyDistillation(distillations);

        var addedDecisionIds = new List<string>();
        foreach (var grp in groupDistillations)
            foreach (var d in grp.Decisions)
                if (_decisions.All(x => x.Id != d.Id))
                {
                    _decisions.Add(new Decision(d.Id, d.Description, d.Impact, grp.GroupId, d.IslandId));
                    addedDecisionIds.Add(d.Id);
                }

        _activeCheckpoint?.DecisionIdsAdded.AddRange(addedDecisionIds);
    }

    /// <summary>Express phase — tracks deliverables planned or produced during distillation.</summary>
    internal void TrackDeliverables(
        IReadOnlyList<DeliverableRecord>        deliverables,
        IReadOnlyList<GroupDistillationRecord>  groupDistillations)
    {
        var addedIds = new List<string>();

        foreach (var del in deliverables)
            if (_deliverables.All(d => d.DeliverableId != del.DeliverableId))
            {
                _deliverables.Add(new Deliverable(del.DeliverableId, del.Path, del.Status));
                addedIds.Add(del.DeliverableId);
            }

        foreach (var grp in groupDistillations)
            foreach (var del in grp.Deliverables)
                if (_deliverables.All(d => d.DeliverableId != del.DeliverableId))
                {
                    _deliverables.Add(new Deliverable(
                        del.DeliverableId, del.Path, del.Status,
                        del.GroupId, del.Purpose, del.IslandIds, del.DecisionIds));
                    addedIds.Add(del.DeliverableId);
                }

        _activeCheckpoint?.DeliverableIdsAdded.AddRange(addedIds);
    }

    // ── Lineage Queries ───────────────────────────────────────────────────────

    /// <summary>
    /// Traces the complete chain for a group: Islands → Decisions → Deliverables.
    /// Use this to answer: "why was this deliverable built?" or "what is at risk if this group changes?"
    /// </summary>
    public BrainLineage GetLineageForGroup(string groupId)
    {
        var group     = _groups.First(g => g.Id == groupId);
        var islands   = _backlog.All.Where(i => group.IslandIds.Contains(i.Id)).ToList();
        var decisions = _decisions
            .Where(d => d.GroupId == groupId || group.IslandIds.Contains(d.IslandId ?? string.Empty))
            .ToList();
        var artifacts = _deliverables.Where(d => d.GroupId == groupId).ToList();
        return new BrainLineage(group, islands, decisions, artifacts);
    }

    /// <summary>Finds every deliverable that was built from a specific Island.</summary>
    public IReadOnlyList<Deliverable> FindDeliverablesByIsland(string islandId)
        => _deliverables.Where(d => d.IslandIds?.Contains(islandId) == true).ToList();

    /// <summary>Returns true when at least one group has reached READY state, enabling Express.</summary>
    public bool IsReadyToExpress()
        => _groups.Any(g => string.Equals(g.Readiness, "READY", StringComparison.OrdinalIgnoreCase));

    // ── Persistence (serialize / restore) ─────────────────────────────────────

    /// <summary>Produces a flat, serializable snapshot of all Brain state for <see cref="AgentFramework.Core.Agent.Ports.IBrainRepository"/>.</summary>
    public BrainState ToState() =>
        new(Id.ProjectId, _backlog.All, _groups.AsReadOnly(),
            _decisions.AsReadOnly(), _deliverables.AsReadOnly(), _checkpoints.AsReadOnly());

    /// <summary>Restores a Brain from a previously serialized <see cref="BrainState"/>.</summary>
    public static BrainAggregate FromState(BrainState state)
    {
        var brain = new BrainAggregate(state.ProjectId);
        brain._backlog.Restore(state.Islands);
        brain._groups.AddRange(state.Groups);
        brain._decisions.AddRange(state.Decisions);
        brain._deliverables.AddRange(state.Deliverables);
        brain._checkpoints.AddRange(state.Checkpoints);
        return brain;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static IReadOnlyList<DecisionRecord> ParseDecisionRecords(JsonElement gd)
    {
        if (!gd.TryGetProperty("decisions", out var decsEl)) return [];
        return decsEl.EnumerateArray().Select(d => new DecisionRecord(
            d.GetProperty("id").GetString()!,
            d.GetProperty("description").GetString()!,
            d.GetProperty("impact").GetString()!,
            d.TryGetProperty("islandId", out var isl) && isl.ValueKind != JsonValueKind.Null ? isl.GetString() : null
        )).ToList().AsReadOnly();
    }

    private static IReadOnlyList<GroupDeliverableRecord> ParseGroupDeliverableRecords(JsonElement gd, string groupId)
    {
        if (!gd.TryGetProperty("deliverables", out var delsEl)) return [];
        return delsEl.EnumerateArray().Select(del =>
        {
            var islandIds = del.TryGetProperty("islandIds", out var iIds) && iIds.ValueKind == JsonValueKind.Array
                ? iIds.EnumerateArray().Select(e => e.GetString()!).ToList().AsReadOnly()
                : (IReadOnlyList<string>?)null;
            var decisionIds = del.TryGetProperty("decisionIds", out var dIds) && dIds.ValueKind == JsonValueKind.Array
                ? dIds.EnumerateArray().Select(e => e.GetString()!).ToList().AsReadOnly()
                : (IReadOnlyList<string>?)null;
            var statusStr = del.TryGetProperty("status", out var s) ? s.GetString() ?? "Draft" : "Draft";
            var status    = Enum.TryParse<DeliverableStatus>(statusStr, ignoreCase: true, out var ds) ? ds : DeliverableStatus.Draft;

            return new GroupDeliverableRecord(
                groupId,
                del.GetProperty("deliverableId").GetString()!,
                del.GetProperty("path").GetString()!,
                del.GetProperty("purpose").GetString()!,
                status, islandIds, decisionIds);
        }).ToList().AsReadOnly();
    }

    // ── Inner: CheckpointBuilder ──────────────────────────────────────────────

    private sealed class CheckpointBuilder(int number, string agentId, string sessionObjective)
    {
        public int      Number           { get; } = number;
        public string   AgentId          { get; } = agentId;
        public string   SessionObjective { get; } = sessionObjective;
        public DateTime CreatedAt        { get; } = DateTime.UtcNow;

        public List<string> IslandIdsAdded      { get; } = [];
        public List<string> GroupIdsAdded        { get; } = [];
        public List<string> DecisionIdsAdded     { get; } = [];
        public List<string> DeliverableIdsAdded  { get; } = [];

        public BrainCheckpoint Build(DateTime closedAt) => new(
            Number, AgentId, SessionObjective, CreatedAt, closedAt,
            IslandIdsAdded.AsReadOnly(), GroupIdsAdded.AsReadOnly(),
            DecisionIdsAdded.AsReadOnly(), DeliverableIdsAdded.AsReadOnly());
    }
}
