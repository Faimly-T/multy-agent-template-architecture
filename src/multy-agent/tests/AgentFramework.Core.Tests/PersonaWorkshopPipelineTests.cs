using System.Text.Json;
using AgentFramework.CodePipeline;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

/// <summary>
/// Full-pipeline integration tests using the athlete student scholarship scenario.
/// All LLM calls are replaced with pre-built fake responses — no Anthropic API required.
///
/// Scenario: "I want to build a system to search and profile potential leads
///            for athlete student scholarships."
///
/// Pipeline result:
///   Kickoff  → objective: define 3 buyer personas for scholarship platform
///   Capture  → 15 islands across Max, Carlos, Claudia
///   Organize → 3 semantic groups (Max / Carlos / Claudia), all Ready
///   Distill  → 3 decisions + 3 deliverables + 2 cross-group questions
///   Express  → token usage recorded
/// </summary>
public class PersonaWorkshopPipelineTests
{
    private const string TestDataPath = "TestData/UxAgentRole.md";

    private static async Task<UxAgent> CreateAgentAsync()
    {
        return await UxAgent.BuildAsync(UxAgentDefaults.Config(TestSteps.DefaultRole()), TestSteps.DefaultResolver());
    }

    // ================================================================
    // Repository path — config + instructions loaded from JSON file
    // ================================================================

    [Fact]
    public async Task LoadConfig_FromJson_BuildsAndRunsFullPipeline()
    {
        var repo    = TestSteps.DefaultRepository();
        var config  = await repo.GetConfigAsync();
        var agent   = await UxAgent.BuildAsync(config!, TestSteps.DefaultResolver(), repo);
        var client  = new ScholarshipFakeClient();
        var results = await agent.ExecuteAllStepsAsync(TestSteps.ScholarshipIntent, client);

        Assert.Equal(6, results.Count);
        Assert.True(agent.IsCompleted);
        Assert.All(results, r => Assert.True(r.GateSatisfied));
    }

    // ================================================================
    // Full pipeline smoke test
    // ================================================================

    [Fact]
    public async Task FullPipeline_ScholarshipScenario_Completes()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        var results = await agent.ExecuteAllStepsAsync(TestSteps.ScholarshipIntent, client);

        Assert.Equal(6, results.Count);
        Assert.True(agent.IsCompleted);
        Assert.All(results, r => Assert.True(r.GateSatisfied));
    }

    // ================================================================
    // Kickoff
    // ================================================================

    [Fact]
    public async Task Kickoff_MapsObjective()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        await agent.ExecuteNextStepAsync(client);

        Assert.Equal(
            "Define 3 buyer personas with pain points and recruitment journey for an athlete student scholarship platform",
            agent.Session!.CurrentCheckpoint!.SessionObjective);
    }

    // ================================================================
    // Capture
    // ================================================================

    [Fact]
    public async Task Capture_Produces15Islands()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        await agent.ExecuteNextStepAsync(client); // step 1
        await agent.ExecuteNextStepAsync(client); // step 2

        Assert.Equal(15, agent.Brain.Islands.Count);
        Assert.All(agent.Brain.Islands, i => Assert.Equal(IslandStatus.Captured, i.Status));
    }

    [Fact]
    public async Task Capture_IslandTypes_AreVaried()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        await agent.ExecuteNextStepAsync(client);
        await agent.ExecuteNextStepAsync(client);

        var types = agent.Brain.Islands.Select(i => i.Type).Distinct().ToList();
        Assert.Contains(IslandType.UserType,          types);
        Assert.Contains(IslandType.PainPoint,         types);
        Assert.Contains(IslandType.BehavioralPattern, types);
        Assert.Contains(IslandType.EmotionalState,    types);
    }

    // ================================================================
    // Organize
    // ================================================================

    [Fact]
    public async Task Organize_Creates3Groups()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        await agent.ExecuteNextStepAsync(client); // kickoff
        await agent.ExecuteNextStepAsync(client); // capture
        await agent.ExecuteNextStepAsync(client); // organize

        Assert.Equal(3, agent.Brain.Groups.Count);
    }

    [Fact]
    public async Task Organize_GroupNames_MatchPersonas()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        for (int i = 0; i < 3; i++) await agent.ExecuteNextStepAsync(client);

        var names = agent.Brain.Groups.Select(g => g.Name).ToList();
        Assert.Contains("Max — Elite Ready",           names);
        Assert.Contains("Carlos — Talented but Blocked", names);
        Assert.Contains("Claudia — Academic First",    names);
    }

    [Fact]
    public async Task Organize_Each_GroupHas5Islands()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        for (int i = 0; i < 3; i++) await agent.ExecuteNextStepAsync(client);

        foreach (var group in agent.Brain.Groups)
            Assert.Equal(5, group.IslandIds.Count);
    }

    [Fact]
    public async Task Organize_AllIslands_BecomeOrganized()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        for (int i = 0; i < 3; i++) await agent.ExecuteNextStepAsync(client);

        Assert.All(agent.Brain.Islands, i => Assert.Equal(IslandStatus.Organized, i.Status));
    }

    [Fact]
    public async Task Organize_Islands_HaveGroupIdSet()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        for (int i = 0; i < 3; i++) await agent.ExecuteNextStepAsync(client);

        Assert.All(agent.Brain.Islands, i => Assert.NotNull(i.GroupId));
    }

    [Fact]
    public async Task Organize_Group1Islands_HaveGRP001()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        for (int i = 0; i < 3; i++) await agent.ExecuteNextStepAsync(client);

        var maxIslands = agent.Brain.Islands.Where(i => i.GroupId == "GRP-001").ToList();
        Assert.All(maxIslands, i => Assert.Equal("GRP-001", i.GroupId));
    }

    [Fact]
    public async Task Organize_ReadinessStatuses_AreSet()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        for (int i = 0; i < 3; i++) await agent.ExecuteNextStepAsync(client);

        var groups = agent.Brain.Groups;
        Assert.Equal("Ready",  groups[0].Readiness);
        Assert.Equal("Ready",  groups[1].Readiness);
        Assert.Equal("OneGap", groups[2].Readiness);
    }

    [Fact]
    public async Task Organize_WhyTogether_IsPopulated()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        for (int i = 0; i < 3; i++) await agent.ExecuteNextStepAsync(client);

        Assert.All(agent.Brain.Groups, g => Assert.False(string.IsNullOrWhiteSpace(g.WhyTogether)));
    }

    // ================================================================
    // Distill
    // ================================================================

    [Fact]
    public async Task Distill_Produces3Decisions_OnePerGroup()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        for (int i = 0; i < 4; i++) await agent.ExecuteNextStepAsync(client);

        Assert.Equal(3, agent.Decisions.Count);
    }

    [Fact]
    public async Task Distill_Decisions_HaveGroupIds()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        for (int i = 0; i < 4; i++) await agent.ExecuteNextStepAsync(client);

        Assert.Contains(agent.Decisions, d => d.GroupId == "GRP-001");
        Assert.Contains(agent.Decisions, d => d.GroupId == "GRP-002");
        Assert.Contains(agent.Decisions, d => d.GroupId == "GRP-003");
    }

    [Fact]
    public async Task Distill_Produces3Deliverables_OnePerGroup()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        for (int i = 0; i < 4; i++) await agent.ExecuteNextStepAsync(client);

        Assert.Equal(3, agent.Deliverables.Count);
    }

    [Fact]
    public async Task Distill_Deliverables_HavePurposeSet()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        for (int i = 0; i < 4; i++) await agent.ExecuteNextStepAsync(client);

        Assert.All(agent.Deliverables, d => Assert.False(string.IsNullOrWhiteSpace(d.Purpose)));
    }

    [Fact]
    public async Task Distill_Deliverables_PathsAreHtml()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        for (int i = 0; i < 4; i++) await agent.ExecuteNextStepAsync(client);

        Assert.All(agent.Deliverables, d => Assert.EndsWith(".html", d.Path));
    }

    [Fact]
    public async Task Distill_Raises2Questions()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        for (int i = 0; i < 4; i++) await agent.ExecuteNextStepAsync(client);

        Assert.Equal(2, agent.Questions.Count);
    }

    [Fact]
    public async Task Distill_Questions_AreOpenAndHaveFeedbackType()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        for (int i = 0; i < 4; i++) await agent.ExecuteNextStepAsync(client);

        Assert.All(agent.Questions, q => Assert.Equal(QuestionStatus.Open, q.Status));
    }

    [Fact]
    public async Task Distill_AllIslands_BecomeDistilled()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        for (int i = 0; i < 4; i++) await agent.ExecuteNextStepAsync(client);

        Assert.All(agent.Brain.Islands, i => Assert.Equal(IslandStatus.Distilled, i.Status));
    }

    // ================================================================
    // Express
    // ================================================================

    [Fact]
    public async Task Express_RecordsTokenConsumption()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        await agent.ExecuteAllStepsAsync(TestSteps.ScholarshipIntent, client);

        Assert.Equal(8_000,  agent.Session!.CurrentCheckpoint!.TokensConsumption.InputTokens);
        Assert.Equal(15_000, agent.Session.CurrentCheckpoint.TokensConsumption.OutputTokens);
        Assert.Equal(23_000, agent.Session.CurrentCheckpoint.TokensConsumption.TotalTokens);
    }

    // ================================================================
    // Full session state after all 5 steps
    // ================================================================

    [Fact]
    public async Task FullPipeline_SessionState_IsFullyPopulated()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        await agent.ExecuteAllStepsAsync(TestSteps.ScholarshipIntent, client);

        // Objective
        Assert.Contains("scholarship", agent.Session!.CurrentCheckpoint!.SessionObjective, StringComparison.OrdinalIgnoreCase);

        // Islands: all 15 distilled
        Assert.Equal(15, agent.Brain.Islands.Count);
        Assert.All(agent.Brain.Islands, i => Assert.Equal(IslandStatus.Distilled, i.Status));

        // Groups: 3
        Assert.Equal(3, agent.Brain.Groups.Count);

        // Decisions: 3 (1 per group, from Distill)
        Assert.Equal(3, agent.Decisions.Count);

        // Deliverables: 3 with HTML paths and purposes
        Assert.Equal(3, agent.Deliverables.Count);
        Assert.All(agent.Deliverables, d =>
        {
            Assert.EndsWith(".html", d.Path);
            Assert.NotNull(d.GroupId);
            Assert.NotNull(d.Purpose);
        });

        // Questions: 2 open from Distill
        Assert.Equal(2, agent.Questions.Count);
        Assert.All(agent.Questions, q => Assert.Equal(QuestionStatus.Open, q.Status));

        // Tokens
        Assert.Equal(23_000, agent.Session.CurrentCheckpoint.TokensConsumption.TotalTokens);
    }

    [Fact]
    public async Task FullPipeline_ConversationAccumulates()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        await agent.ExecuteAllStepsAsync(TestSteps.ScholarshipIntent, client);

        Assert.NotEmpty(agent.GetStepJournal("KickoffStep"));
        Assert.NotEmpty(agent.GetStepJournal("ClosedStep"));
    }

    [Fact]
    public async Task FullPipeline_SupplyAnswers_WorksAfterDistill()
    {
        var agent  = await CreateAgentAsync();
        var client = new ScholarshipFakeClient();

        await agent.ExecuteAllStepsAsync(TestSteps.ScholarshipIntent, client);

        // User answers both distill questions
        agent.SupplyAnswers([
            ("UX-Q001", "Use AI-based matching with optional manual range override", "PjM Review"),
            ("UX-Q002", "Yes — create parent/guardian persona for COPPA compliance", "Legal Review")
        ]);

        Assert.Equal(2, agent.GetPendingReviewQuestions().Count);
        Assert.Empty(agent.GetOpenQuestions());
    }

    // ================================================================
    // OrganizeStep.ParseResult — unit tests
    // ================================================================

    [Fact]
    public void OrganizeStep_ParseResult_ExtractsGroups()
    {
        var step = new OrganizeStep(Core.Agent.Prompts.PromptContext.Empty, 3, "organize", new Gate("≥1 Ready group"));
        using var doc  = JsonDocument.Parse(ScholarshipFakeClient.OrganizeJson);
        var result     = step.ParseResult(doc.RootElement, ScholarshipFakeClient.OrganizeJson, true);
        var organize   = Assert.IsType<OrganizeResult>(result);

        Assert.Equal(3, organize.Groups!.Count);
        Assert.Equal("GRP-001", organize.Groups[0].Id);
        Assert.Equal("Max — Elite Ready", organize.Groups[0].Name);
        Assert.Equal(5, organize.Groups[0].IslandIds.Count);
    }

    [Fact]
    public void OrganizeStep_ParseResult_ExtractsOrganizedIslands()
    {
        var step = new OrganizeStep(Core.Agent.Prompts.PromptContext.Empty, 3, "organize", new Gate("≥1 Ready group"));
        using var doc  = JsonDocument.Parse(ScholarshipFakeClient.OrganizeJson);
        var result     = step.ParseResult(doc.RootElement, ScholarshipFakeClient.OrganizeJson, true);
        var organize   = Assert.IsType<OrganizeResult>(result);

        Assert.Equal(15, organize.OrganizedIslands.Count);
        var first = organize.OrganizedIslands[0];
        Assert.Equal("ISL-001", first.IslandId);
        Assert.Equal(IslandStatus.Organized, first.NewStatus);
        Assert.Equal("GRP-001", first.GroupId);
    }

    // ================================================================
    // DistillStep.ParseResult — unit tests
    // ================================================================

    [Fact]
    public void DistillStep_ParseResult_ExtractsGroupDistillations()
    {
        var step = new DistillStep(Core.Agent.Prompts.PromptContext.Empty, 4, "distill", new Gate("All groups distilled"));
        using var doc  = JsonDocument.Parse(ScholarshipFakeClient.DistillJson);
        var result     = step.ParseResult(doc.RootElement, ScholarshipFakeClient.DistillJson, true);
        var distill    = Assert.IsType<DistillResult>(result);

        Assert.Equal(3, distill.GroupDistillations!.Count);
        Assert.Equal("GRP-001", distill.GroupDistillations[0].GroupId);
        Assert.Single(distill.GroupDistillations[0].Decisions);
        Assert.Single(distill.GroupDistillations[0].Deliverables);
        Assert.Empty(distill.GroupDistillations[0].Questions);
    }

    [Fact]
    public void DistillStep_ParseResult_ExtractsQuestionsFromGroup3()
    {
        var step = new DistillStep(Core.Agent.Prompts.PromptContext.Empty, 4, "distill", new Gate("All groups distilled"));
        using var doc  = JsonDocument.Parse(ScholarshipFakeClient.DistillJson);
        var result     = step.ParseResult(doc.RootElement, ScholarshipFakeClient.DistillJson, true);
        var distill    = Assert.IsType<DistillResult>(result);

        var grp3 = distill.GroupDistillations!.First(g => g.GroupId == "GRP-003");
        Assert.Equal(2, grp3.Questions.Count);
        Assert.Equal("UX-Q001", grp3.Questions[0].Id);
        Assert.Equal("FeedbackRequest", grp3.Questions[0].QuestionType);
        Assert.Contains("PjM", grp3.Questions[0].TargetRoles);
    }

    [Fact]
    public void DistillStep_ParseResult_Extracts15DistilledIslands()
    {
        var step = new DistillStep(Core.Agent.Prompts.PromptContext.Empty, 4, "distill", new Gate("All groups distilled"));
        using var doc  = JsonDocument.Parse(ScholarshipFakeClient.DistillJson);
        var result     = step.ParseResult(doc.RootElement, ScholarshipFakeClient.DistillJson, true);
        var distill    = Assert.IsType<DistillResult>(result);

        Assert.Equal(15, distill.DistilledIslands.Count);
        Assert.All(distill.DistilledIslands, i => Assert.Equal(IslandStatus.Distilled, i.NewStatus));
    }

    [Fact]
    public void DistillStep_ParseResult_GroupDeliverable_HasPurpose()
    {
        var step = new DistillStep(Core.Agent.Prompts.PromptContext.Empty, 4, "distill", new Gate("All groups distilled"));
        using var doc  = JsonDocument.Parse(ScholarshipFakeClient.DistillJson);
        var result     = step.ParseResult(doc.RootElement, ScholarshipFakeClient.DistillJson, true);
        var distill    = Assert.IsType<DistillResult>(result);

        var del = distill.GroupDistillations![0].Deliverables[0];
        Assert.Equal("DEL-001", del.DeliverableId);
        Assert.Equal("GRP-001", del.GroupId);
        Assert.Contains("Max", del.Purpose);
    }

    // ================================================================
    // Fake chat client
    // ================================================================

    private sealed class ScholarshipFakeClient : IChatClient
    {
        // --- Shared JSON constants (used in unit tests too) ---

        public static readonly string OrganizeJson = BuildOrganizeJson();
        public static readonly string DistillJson  = BuildDistillJson();

        // --- SendHandlerAsync routing ---

        public Task<TResult> SendHandlerAsync<TResult>(
            IReadOnlyList<ChatMessage> messages, string jsonSchema,
            Func<JsonElement, TResult> parse, CancellationToken ct = default)
        {
            var json = RouteSchema(jsonSchema);
            return Task.FromResult(parse(JsonDocument.Parse(json).RootElement));
        }

        private static string RouteSchema(string schema)
        {
            if (schema.Contains("triaged"))
                return """{"triaged":[]}""";

            if (schema.Contains("groupDistillations"))
                return DistillJson;

            if (schema.Contains("readiness"))
                return BuildGroupsWithReadinessJson();

            if (schema.Contains("islandIds"))
                return BuildGroupsJson();

            if (schema.Contains("islands"))
                return BuildIslandsJson();

            // UxExpress content handlers — LLM returns HTML directly
            if (schema.Contains("\"html\""))
                return """{"html":"<html><body>Test Document</body></html>"}""";

            // UxQuestionReviewHandler — tokens + question statuses
            if (schema.Contains("inputTokens"))
                return """{"questions":[],"inputTokens":8000,"outputTokens":15000,"gateSatisfied":true}""";

            // Kickoff synthesis (ObjectiveSynthesisHandler)
            return """{"sessionObjective":"Define 3 buyer personas with pain points and recruitment journey for an athlete student scholarship platform","narrativeBridge":"Initial session — no prior context.","isInitialSession":true,"stalenessWarning":null,"gateSatisfied":true}""";
        }

        // --- SendAsync (only called by Express step — no chain) ---

        public Task<StepResult> SendAsync(
            IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            if (step.StepNumber == 5)
                return Task.FromResult<StepResult>(new ExpressResult(
                    Output: """{"inputTokens":8000,"outputTokens":15000,"gateSatisfied":true}""",
                    GateSatisfied: true,
                    InputTokens: 8_000,
                    OutputTokens: 15_000,
                    Questions: []));

            throw new InvalidOperationException(
                $"SendAsync called unexpectedly for step {step.StepNumber} — should use chain.");
        }

        // ----------------------------------------------------------------
        // JSON builders
        // ----------------------------------------------------------------

        private static string BuildIslandsJson() => """
            {"islands":[
              {"id":"ISL-001","type":"UserType","description":"Max, 15yr Colombian soccer player with elite performance records","source":"six-hats:white","relatesToIslandId":null},
              {"id":"ISL-002","type":"BehavioralPattern","description":"Max has official match videos and tournament stats","source":"six-hats:white","relatesToIslandId":"ISL-001"},
              {"id":"ISL-003","type":"BehavioralPattern","description":"Max speaks conversational English and maintains B+ grades","source":"six-hats:yellow","relatesToIslandId":"ISL-001"},
              {"id":"ISL-004","type":"ContextOfUse","description":"Max family can partially fund US education up to $15K/yr","source":"six-hats:white","relatesToIslandId":"ISL-001"},
              {"id":"ISL-005","type":"PainPoint","description":"Max fears no US college scout will ever find him without an agent","source":"six-hats:black","relatesToIslandId":"ISL-001"},
              {"id":"ISL-006","type":"UserType","description":"Carlos, 16yr Colombian soccer player at national youth team level","source":"six-hats:white","relatesToIslandId":null},
              {"id":"ISL-007","type":"PainPoint","description":"Carlos has limited English and needs ESL bridge programs","source":"six-hats:black","relatesToIslandId":"ISL-006"},
              {"id":"ISL-008","type":"PainPoint","description":"Carlos family has zero budget for US tuition or travel","source":"six-hats:black","relatesToIslandId":"ISL-006"},
              {"id":"ISL-009","type":"PainPoint","description":"Carlos has no digital athletic profile or highlight reel","source":"six-hats:black","relatesToIslandId":"ISL-006"},
              {"id":"ISL-010","type":"EmotionalState","description":"Carlos fears language barrier will disqualify him before any coach sees his skills","source":"six-hats:red","relatesToIslandId":"ISL-006"},
              {"id":"ISL-011","type":"UserType","description":"Claudia, 17yr top academic student who is not a competitive athlete","source":"six-hats:white","relatesToIslandId":null},
              {"id":"ISL-012","type":"BehavioralPattern","description":"Claudia speaks excellent English and has SAT 1450+ score","source":"six-hats:yellow","relatesToIslandId":"ISL-011"},
              {"id":"ISL-013","type":"ContextOfUse","description":"Claudia family can fully fund university tuition","source":"six-hats:white","relatesToIslandId":"ISL-011"},
              {"id":"ISL-014","type":"Goal","description":"Claudia wants academic + sports scholarship but has no athletic profile","source":"six-hats:green","relatesToIslandId":"ISL-011"},
              {"id":"ISL-015","type":"EmotionalState","description":"Claudia feels excluded when scholarship platforms are athlete-only","source":"six-hats:red","relatesToIslandId":"ISL-011"}
            ]}
            """;

        private static string BuildGroupsJson() => """
            {"groups":[
              {"id":"GRP-001","name":"Max — Elite Ready","islandIds":["ISL-001","ISL-002","ISL-003","ISL-004","ISL-005"],"whyTogether":"These islands describe a high-potential athlete positioned for US scholarships but lacking visibility"},
              {"id":"GRP-002","name":"Carlos — Talented but Blocked","islandIds":["ISL-006","ISL-007","ISL-008","ISL-009","ISL-010"],"whyTogether":"These islands describe exceptional talent blocked by language, financial, and digital-presence barriers"},
              {"id":"GRP-003","name":"Claudia — Academic First","islandIds":["ISL-011","ISL-012","ISL-013","ISL-014","ISL-015"],"whyTogether":"These islands describe a high-achieving student excluded from athletic scholarship paths"}
            ],"ungroupedIslandIds":[]}
            """;

        private static string BuildGroupsWithReadinessJson() => """
            {"groups":[
              {"id":"GRP-001","name":"Max — Elite Ready","islandIds":["ISL-001","ISL-002","ISL-003","ISL-004","ISL-005"],"whyTogether":"These islands describe a high-potential athlete positioned for US scholarships but lacking visibility","readiness":"Ready","readinessNotes":null},
              {"id":"GRP-002","name":"Carlos — Talented but Blocked","islandIds":["ISL-006","ISL-007","ISL-008","ISL-009","ISL-010"],"whyTogether":"These islands describe exceptional talent blocked by language, financial, and digital-presence barriers","readiness":"Ready","readinessNotes":null},
              {"id":"GRP-003","name":"Claudia — Academic First","islandIds":["ISL-011","ISL-012","ISL-013","ISL-014","ISL-015"],"whyTogether":"These islands describe a high-achieving student excluded from athletic scholarship paths","readiness":"OneGap","readinessNotes":"HOW dimension needs definition — no direction on handling academic-only applicants"}
            ]}
            """;

        private static string BuildOrganizeJson()
        {
            var islands = string.Join(",", Enumerable.Range(1, 15).Select(n =>
            {
                var grp = n <= 5 ? "GRP-001" : n <= 10 ? "GRP-002" : "GRP-003";
                return $$$"""{"islandId":"ISL-{{{n:D3}}}","newStatus":"Organized","groupId":"{{{grp}}}"}""";
            }));

            return $$"""
                {"groups":[
                  {"id":"GRP-001","name":"Max — Elite Ready","islandIds":["ISL-001","ISL-002","ISL-003","ISL-004","ISL-005"],"whyTogether":"High-potential athlete lacking visibility","readiness":"Ready","readinessNotes":null},
                  {"id":"GRP-002","name":"Carlos — Talented but Blocked","islandIds":["ISL-006","ISL-007","ISL-008","ISL-009","ISL-010"],"whyTogether":"Exceptional talent blocked by non-athletic barriers","readiness":"Ready","readinessNotes":null},
                  {"id":"GRP-003","name":"Claudia — Academic First","islandIds":["ISL-011","ISL-012","ISL-013","ISL-014","ISL-015"],"whyTogether":"Academic achiever excluded from athlete-only paths","readiness":"OneGap","readinessNotes":"HOW dimension missing"}
                ],"organizedIslands":[{{islands}}],"gateSatisfied":true}
                """;
        }

        private static string BuildDistillJson() => """
            {"groupDistillations":[
              {
                "groupId":"GRP-001",
                "decisions":[
                  {"id":"DEC-001","description":"Max persona: 15yr elite Colombian soccer player with partial family budget — defined as the premium acquisition target with visibility as the primary unlock","impact":"Highest conversion probability — maps directly to core market segment"}
                ],
                "deliverables":[
                  {"deliverableId":"DEL-001","path":"outputs/personas/01-max-elite-ready.html","purpose":"Full buyer persona and recruitment journey map for Max — the ideal scholarship candidate with clear WHY, HOW, WHAT","status":"Draft"}
                ],
                "questions":[]
              },
              {
                "groupId":"GRP-002",
                "decisions":[
                  {"id":"DEC-002","description":"Carlos persona: exceptional talent with language and financial constraints — platform must include ESL bridge matching and need-based financial aid discovery","impact":"Opens underserved high-talent segment blocked by non-athletic barriers"}
                ],
                "deliverables":[
                  {"deliverableId":"DEL-002","path":"outputs/personas/02-carlos-constrained.html","purpose":"Buyer persona and barrier map for Carlos — the high-talent financially constrained athlete who needs non-athletic support services","status":"Draft"}
                ],
                "questions":[]
              },
              {
                "groupId":"GRP-003",
                "decisions":[
                  {"id":"DEC-003","description":"Claudia persona: academic-first applicant — platform needs inclusive discovery mode surfacing academic + athletic hybrid scholarships","impact":"Expands TAM beyond pure athletes to high-achieving academic students with strong financial profiles"}
                ],
                "deliverables":[
                  {"deliverableId":"DEL-003","path":"outputs/personas/03-claudia-academic.html","purpose":"Buyer persona and journey for Claudia — the academic-first student seeking scholarship visibility in platforms built for athletes","status":"Draft"}
                ],
                "questions":[
                  {"id":"UX-Q001","text":"Should the economic range filter be configurable by the user or use AI-based scholarship matching?","questionType":"FeedbackRequest","targetRoles":["PjM","Engineering"]},
                  {"id":"UX-Q002","text":"Should we define a parent/guardian sub-persona given COPPA compliance requirements for users under 18?","questionType":"FeedbackRequest","targetRoles":["Legal","PjM"]}
                ]
              }
            ],
            "distilledIslands":[
              {"islandId":"ISL-001","newStatus":"Distilled"},{"islandId":"ISL-002","newStatus":"Distilled"},{"islandId":"ISL-003","newStatus":"Distilled"},
              {"islandId":"ISL-004","newStatus":"Distilled"},{"islandId":"ISL-005","newStatus":"Distilled"},{"islandId":"ISL-006","newStatus":"Distilled"},
              {"islandId":"ISL-007","newStatus":"Distilled"},{"islandId":"ISL-008","newStatus":"Distilled"},{"islandId":"ISL-009","newStatus":"Distilled"},
              {"islandId":"ISL-010","newStatus":"Distilled"},{"islandId":"ISL-011","newStatus":"Distilled"},{"islandId":"ISL-012","newStatus":"Distilled"},
              {"islandId":"ISL-013","newStatus":"Distilled"},{"islandId":"ISL-014","newStatus":"Distilled"},{"islandId":"ISL-015","newStatus":"Distilled"}
            ],
            "gateSatisfied":true}
            """;
    }
}
