using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Handlers;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.CodePipeline;

public class DistillStep : AgentStep
{
    public DistillStep(
        IStepPromptLayer       stepContext,
        int                    stepNumber,
        string                 instructions,
        Gate                   gate,
        IStepChain?            chain             = null,
        Func<string, string?>? instructionLookup = null)
        : base(stepContext, stepNumber, instructions, gate, chain, instructionLookup) { }

    /// <summary>Gate: at least one group has been distilled and at least one island is distilled.</summary>
    protected override bool EvaluateGate(JsonElement root, IReadOnlyList<HandlerExchange> journal)
    {
        var hasGroups  = root.TryGetProperty("groupDistillations", out var gd) && gd.GetArrayLength() > 0;
        var hasIslands = root.TryGetProperty("distilledIslands",   out var di) && di.GetArrayLength() > 0;
        return hasGroups && hasIslands;
    }

    public override string JsonResponseSchema => """
        {
          "groupDistillations": [
            {
              "groupId": "GRP-001",
              "decisions": [
                { "id": "DEC-001", "description": "string", "impact": "string" }
              ],
              "deliverables": [
                { "deliverableId": "DEL-001", "path": "outputs/folder/file.html", "purpose": "string", "status": "Draft | Partial | Complete" }
              ],
              "questions": [
                { "id": "UX-Q001", "text": "string", "questionType": "Doubt | FeedbackRequest | IdeaValidation", "targetRoles": ["PjM"] }
              ]
            }
          ],
          "distilledIslands": [
            { "islandId": "ISL-XXX", "newStatus": "Distilled | Discarded" }
          ],
          "gateSatisfied": true
        }
        """;

    public override string BuildContext(IAgentRunContext? context)
    {
        if (context is null) return "No session context";

        var groups    = context.Brain?.Groups ?? [];
        var organized = context.Brain?.Backlog.GetByStatus(IslandStatus.Organized) ?? [];
        var decisions = context.Decisions;

        var groupLines = groups.Select(g =>
        {
            var islands = organized.Where(i => i.GroupId == g.Id)
                .Select(i => $"  - {i.Id} [{i.Type}] {i.Description}");
            return $"Group [{g.Id}] {g.Name} ({g.Readiness})\n  WHY: {g.WhyTogether}\n{string.Join("\n", islands)}";
        });

        var ungrouped = organized.Where(i => i.GroupId is null)
            .Select(i => $"  - {i.Id} [{i.Type}] {i.Description}");

        return $"""
            Groups and their islands:
            {string.Join("\n\n", groupLines)}

            {(ungrouped.Any() ? $"Ungrouped islands:\n{string.Join("\n", ungrouped)}" : string.Empty)}

            Existing decisions:
            {(decisions.Any() ? string.Join("\n", decisions.Select(d => $"- {d.Id}: {d.Description}")) : "None")}
            """;
    }

    public override StepResult ParseResult(JsonElement root, string rawOutput, bool gateSatisfied)
    {
        var distilled = new List<IslandDistillation>();
        if (root.TryGetProperty("distilledIslands", out var di))
        {
            foreach (var el in di.EnumerateArray())
            {
                var id        = el.TryGetProperty("islandId",  out var idEl) ? idEl.GetString()  ?? string.Empty : string.Empty;
                var statusStr = el.TryGetProperty("newStatus", out var stEl) ? stEl.GetString()  ?? "Distilled"  : "Distilled";
                var status    = Enum.TryParse<IslandStatus>(statusStr, ignoreCase: true, out var s) ? s : IslandStatus.Distilled;
                if (!string.IsNullOrWhiteSpace(id))
                    distilled.Add(new IslandDistillation(id, status));
            }
        }

        var deliverables = new List<DeliverableRecord>();
        if (root.TryGetProperty("deliverables", out var del))
        {
            foreach (var el in del.EnumerateArray())
            {
                var delivId   = el.TryGetProperty("deliverableId", out var dEl)  ? dEl.GetString()  ?? string.Empty : string.Empty;
                var path      = el.TryGetProperty("path",          out var pEl)  ? pEl.GetString()  ?? string.Empty : string.Empty;
                var statusStr = el.TryGetProperty("status",        out var stEl) ? stEl.GetString() ?? "Draft"      : "Draft";
                var status    = Enum.TryParse<DeliverableStatus>(statusStr, ignoreCase: true, out var s) ? s : DeliverableStatus.Draft;
                if (!string.IsNullOrWhiteSpace(delivId))
                    deliverables.Add(new DeliverableRecord(delivId, path, status));
            }
        }

        var groupDistillations = new List<GroupDistillationRecord>();
        if (root.TryGetProperty("groupDistillations", out var gdArr))
        {
            foreach (var grpEl in gdArr.EnumerateArray())
            {
                var groupId = grpEl.TryGetProperty("groupId", out var gEl) ? gEl.GetString() ?? string.Empty : string.Empty;

                var grpDecisions = new List<DecisionRecord>();
                if (grpEl.TryGetProperty("decisions", out var decArr))
                    foreach (var d in decArr.EnumerateArray())
                    {
                        var id     = d.TryGetProperty("id",          out var iEl) ? iEl.GetString() ?? string.Empty : string.Empty;
                        var desc   = d.TryGetProperty("description", out var dEl) ? dEl.GetString() ?? string.Empty : string.Empty;
                        var impact = d.TryGetProperty("impact",      out var pEl) ? pEl.GetString() ?? string.Empty : string.Empty;
                        if (!string.IsNullOrWhiteSpace(id)) grpDecisions.Add(new DecisionRecord(id, desc, impact));
                    }

                var grpDeliverables = new List<GroupDeliverableRecord>();
                if (grpEl.TryGetProperty("deliverables", out var delArr))
                    foreach (var d in delArr.EnumerateArray())
                    {
                        var delivId   = d.TryGetProperty("deliverableId", out var iEl)  ? iEl.GetString()  ?? string.Empty : string.Empty;
                        var path      = d.TryGetProperty("path",          out var pEl)  ? pEl.GetString()  ?? string.Empty : string.Empty;
                        var purpose   = d.TryGetProperty("purpose",       out var puEl) ? puEl.GetString() ?? string.Empty : string.Empty;
                        var statusStr = d.TryGetProperty("status",        out var stEl) ? stEl.GetString() ?? "Draft"      : "Draft";
                        var status    = Enum.TryParse<DeliverableStatus>(statusStr, ignoreCase: true, out var st) ? st : DeliverableStatus.Draft;
                        if (!string.IsNullOrWhiteSpace(delivId))
                            grpDeliverables.Add(new GroupDeliverableRecord(groupId, delivId, path, purpose, status));
                    }

                var grpQuestions = new List<GroupQuestionRecord>();
                if (grpEl.TryGetProperty("questions", out var qArr))
                    foreach (var q in qArr.EnumerateArray())
                    {
                        var id   = q.TryGetProperty("id",           out var iEl)   ? iEl.GetString()   ?? string.Empty : string.Empty;
                        var text = q.TryGetProperty("text",         out var tEl)   ? tEl.GetString()   ?? string.Empty : string.Empty;
                        var qType= q.TryGetProperty("questionType", out var qtEl)  ? qtEl.GetString()  ?? "Doubt"      : "Doubt";
                        var roles= new List<string>();
                        if (q.TryGetProperty("targetRoles", out var rolesArr))
                            foreach (var r in rolesArr.EnumerateArray())
                                if (r.GetString() is { Length: > 0 } role) roles.Add(role);
                        if (!string.IsNullOrWhiteSpace(id))
                            grpQuestions.Add(new GroupQuestionRecord(groupId, id, text, qType, roles.AsReadOnly()));
                    }

                if (!string.IsNullOrWhiteSpace(groupId))
                    groupDistillations.Add(new GroupDistillationRecord(groupId, grpDecisions, grpDeliverables, grpQuestions));
            }
        }

        return new DistillResult(rawOutput, gateSatisfied, distilled, deliverables, groupDistillations);
    }
}

public record DistillResult(
    string Output,
    bool GateSatisfied,
    IReadOnlyList<IslandDistillation> DistilledIslands,
    IReadOnlyList<DeliverableRecord> Deliverables,
    IReadOnlyList<GroupDistillationRecord>? GroupDistillations = null) : StepResult(Output, GateSatisfied)
{
    public override void ApplyTo(ISessionWriter writer)
    {
        // Brain: advance island statuses + record group-level decisions
        writer.ApplyDistillation(DistilledIslands, GroupDistillations ?? []);

        // Deliverable tracker: record output artifacts
        writer.TrackDeliverables(Deliverables, GroupDistillations ?? []);

        // Questions raised during distillation
        foreach (var grp in GroupDistillations ?? [])
            foreach (var q in grp.Questions)
                writer.RaiseQuestion(q.Id, q.Text, $"distill:{grp.GroupId}");
    }
}
