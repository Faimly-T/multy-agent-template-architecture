using System.Text.Json;
using AgentFramework.CodePipeline;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

/// <summary>
/// Verifies the multi-iteration pipeline contract for <see cref="UxAgent"/>:
/// - Each call to RunAsync creates a Checkpoint that stores both UserIntent and SessionObjective.
/// - ClosedStep seals the Checkpoint with a ClosedAt timestamp.
/// - PrepareNextIteration resets the pipeline so a second full 6-step run is possible.
/// - Kickoff in iteration 2 receives the prior Checkpoint context.
///
/// Scenario: UX research on 3 athlete-student journeys (recruitment, onboarding, academic support).
/// </summary>
public class UxAgentMultiIterationTests
{
    private const string TestDataPath = "TestData/UxAgentRole.md";

    private static async Task<UxAgent> CreateAgentAsync()
    {
        return await UxAgent.BuildAsync(UxAgentDefaults.Config(TestSteps.DefaultRole()), TestSteps.DefaultResolver());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Checkpoint field tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FirstIteration_Checkpoint_StoresUserIntentAndObjective()
    {
        var persona = await CreateAgentAsync();
        var intent  = "Map 3 athlete-student journeys: recruitment, onboarding, academic support";

        await persona.RunAsync(intent, new Iteration1FakeClient(), NullDeliverableWriter.Instance);

        var cp = persona.Session!.Checkpoints[0];
        Assert.Equal(intent, cp.UserIntent);
        Assert.NotEmpty(cp.SessionObjective);
    }

    [Fact]
    public async Task FirstIteration_Checkpoint_IsSealedByClosedStep()
    {
        var persona = await CreateAgentAsync();

        await persona.RunAsync(
            "Map athlete-student journeys",
            new Iteration1FakeClient(),
            NullDeliverableWriter.Instance);

        Assert.NotNull(persona.Session!.Checkpoints[0].ClosedAt);
    }

    [Fact]
    public async Task FirstIteration_Checkpoint_HasSessionIteration1()
    {
        var persona = await CreateAgentAsync();

        await persona.RunAsync(
            "Map athlete journeys",
            new Iteration1FakeClient(),
            NullDeliverableWriter.Instance);

        Assert.Equal(1, persona.Session!.Checkpoints[0].SessionIteration);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Two-iteration accumulation tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TwoIterations_CheckpointListGrowsToTwo()
    {
        var persona  = await CreateAgentAsync();
        var writer   = NullDeliverableWriter.Instance;

        await persona.RunAsync("Map athlete journeys", new Iteration1FakeClient(), writer);
        persona.PrepareNextIteration();
        await persona.RunAsync("Deepen recruitment persona", new Iteration2FakeClient(), writer);

        Assert.Equal(2, persona.Session!.Checkpoints.Count);
    }

    [Fact]
    public async Task TwoIterations_SecondCheckpoint_HasCorrectIterationAndIntent()
    {
        var persona  = await CreateAgentAsync();
        var writer   = NullDeliverableWriter.Instance;
        var intent2  = "Deepen recruitment journey persona with coach Q&A answers";

        await persona.RunAsync("Map athlete journeys", new Iteration1FakeClient(), writer);
        persona.PrepareNextIteration();
        await persona.RunAsync(intent2, new Iteration2FakeClient(), writer);

        var cp2 = persona.Session!.Checkpoints[1];
        Assert.Equal(2, cp2.SessionIteration);
        Assert.Equal(intent2, cp2.UserIntent);
        Assert.NotEmpty(cp2.SessionObjective);
        Assert.NotNull(cp2.ClosedAt);
    }

    [Fact]
    public async Task TwoIterations_FirstCheckpointUnchangedAfterSecondRun()
    {
        var persona  = await CreateAgentAsync();
        var writer   = NullDeliverableWriter.Instance;
        var intent1  = "Map athlete journeys";

        await persona.RunAsync(intent1, new Iteration1FakeClient(), writer);
        var cp1Before = persona.Session!.Checkpoints[0];

        persona.PrepareNextIteration();
        await persona.RunAsync("Deepen", new Iteration2FakeClient(), writer);

        var cp1After = persona.Session!.Checkpoints[0];
        Assert.Equal(cp1Before.UserIntent,        cp1After.UserIntent);
        Assert.Equal(cp1Before.SessionObjective,   cp1After.SessionObjective);
        Assert.Equal(cp1Before.SessionIteration,   cp1After.SessionIteration);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Kickoff context propagation test
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SecondIteration_KickoffReceivesPriorCheckpointContext()
    {
        var persona  = await CreateAgentAsync();
        var writer   = NullDeliverableWriter.Instance;

        await persona.RunAsync("Map athlete journeys", new Iteration1FakeClient(), writer);
        persona.PrepareNextIteration();

        // Iteration 2 client captures the prompts sent to CheckpointValidatorCommand
        var capturingClient = new CapturingIteration2FakeClient();
        await persona.RunAsync("Deepen recruitment persona", capturingClient, writer);

        // The prompt sent for checkpoint validation should contain Checkpoint #1 details
        Assert.Contains("Checkpoint #1", capturingClient.CapturedCheckpointValidatorPrompt);
        Assert.Contains("Map athlete journeys", capturingClient.CapturedCheckpointValidatorPrompt);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PrepareNextIteration — answer questions before second run
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PrepareNextIteration_WithAnswers_MarksQuestionsAnswered()
    {
        var persona  = await CreateAgentAsync();
        var writer   = NullDeliverableWriter.Instance;
        var client1  = new Iteration1WithQuestionsClient();

        await persona.RunAsync("Map athlete journeys", client1, writer);

        // Iteration 1 raises Q-ATH-001 via the express handler
        Assert.Contains(persona.Questions, q => q.Id == "Q-ATH-001");

        persona.PrepareNextIteration(answers:
        [
            ("Q-ATH-001", "Scholarship athletes only — walk-ons use separate portal", "coach-interview")
        ]);

        Assert.Equal(QuestionStatus.Answered, persona.FindQuestion("Q-ATH-001")!.Status);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Guard rails
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PrepareNextIteration_BeforeRunCompletes_Throws()
    {
        var persona = await CreateAgentAsync();

        // Don't run — pipeline is at step 0, not completed
        Assert.Throws<InvalidOperationException>(() => persona.PrepareNextIteration());
    }

    [Fact]
    public async Task RunAsync_AfterCompletionWithoutPrepare_Throws()
    {
        var persona = await CreateAgentAsync();
        await persona.RunAsync("Map athlete journeys", new Iteration1FakeClient(), NullDeliverableWriter.Instance);

        // Pipeline is now completed — second RunAsync without PrepareNextIteration must throw
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            persona.RunAsync("Second intent", new Iteration2FakeClient(), NullDeliverableWriter.Instance));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Fake chat clients
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Full iteration 1 — all steps respond successfully.</summary>
    private class Iteration1FakeClient : FakeMultiStepClient
    {
        protected override string KickoffObjective =>
            "Map and document 3 athlete-student journeys for the recruiting platform. " +
            "Success = 3 distinct journey maps. Stakes: UX design blocked without these.";

        protected override bool IsInitialSession => true;
    }

    /// <summary>Full iteration 2 — isInitialSession=false, narrative bridge references checkpoint #1.</summary>
    private class Iteration2FakeClient : FakeMultiStepClient
    {
        protected override string KickoffObjective =>
            "Deepen the recruitment journey persona using coach Q&A. " +
            "Success = enriched persona with validated pain points. Stakes: interview script blocked.";

        protected override bool IsInitialSession => false;

        protected override string NarrativeBridge =>
            "Building on Checkpoint #1 where 3 journeys were mapped. " +
            "Recruitment persona needs deeper coach perspective from answered questions.";
    }

    /// <summary>Same as Iteration1FakeClient but raises a question during Express.</summary>
    private class Iteration1WithQuestionsClient : Iteration1FakeClient
    {
        protected override string ExpressReviewJson => """
            {
              "questions": [
                { "id": "Q-ATH-001", "text": "Are walk-on athletes included in the portal?", "status": "open" }
              ],
              "inputTokens": 100,
              "outputTokens": 200,
              "gateSatisfied": true
            }
            """;
    }

    /// <summary>
    /// Iteration 2 client that also captures the user content sent to CheckpointValidatorCommand
    /// (identified by the presence of "Previous checkpoints:" in the user turn).
    /// </summary>
    private class CapturingIteration2FakeClient : Iteration2FakeClient
    {
        public string CapturedCheckpointValidatorPrompt { get; private set; } = string.Empty;

        public override Task<TResult> SendHandlerAsync<TResult>(
            IReadOnlyList<ChatMessage> messages,
            string jsonSchema,
            Func<JsonElement, TResult> parse,
            CancellationToken ct = default)
        {
            var userTurn = messages.FirstOrDefault(m => m.Role == MessageRole.User)?.Content ?? "";
            if (userTurn.Contains("Previous checkpoints:") &&
                string.IsNullOrEmpty(CapturedCheckpointValidatorPrompt))
            {
                CapturedCheckpointValidatorPrompt = userTurn;
            }
            return base.SendHandlerAsync(messages, jsonSchema, parse, ct);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Base fake client — handles all 6 steps
    // ─────────────────────────────────────────────────────────────────────────

    private abstract class FakeMultiStepClient : IChatClient
    {
        protected virtual string KickoffObjective    => "Map athlete journeys. Success = 3 personas.";
        protected virtual bool   IsInitialSession    => true;
        protected virtual string NarrativeBridge     => "Initial session — starting fresh.";
        protected virtual string ExpressReviewJson   => """
            {
              "questions": [],
              "inputTokens": 150,
              "outputTokens": 300,
              "gateSatisfied": true
            }
            """;

        private string KickoffJson => $$"""
            {
              "sessionObjective": "{{KickoffObjective}}",
              "narrativeBridge": "{{NarrativeBridge}}",
              "isInitialSession": {{IsInitialSession.ToString().ToLower()}},
              "stalenessWarning": null,
              "blockers": [],
              "gateSatisfied": true
            }
            """;

        private const string CaptureJson = """
            {
              "islands": [
                {"id":"ISL-001","type":"UserType","description":"Athlete in recruitment journey","source":"objective","relatesToIslandId":null},
                {"id":"ISL-002","type":"UserType","description":"Athlete in onboarding journey","source":"objective","relatesToIslandId":null},
                {"id":"ISL-003","type":"UserType","description":"Athlete in academic support journey","source":"objective","relatesToIslandId":null}
              ],
              "gateSatisfied": true
            }
            """;

        // IslandGroupingHandler schema (has ungroupedIslandIds)
        private const string GroupingJson = """
            {
              "groups": [
                {"id":"GRP-001","name":"Active Recruitment","islandIds":["ISL-001","ISL-002"],"whyTogether":"Both relate to recruiting"},
                {"id":"GRP-002","name":"Support","islandIds":["ISL-003"],"whyTogether":"Academic support is distinct"}
              ],
              "ungroupedIslandIds": []
            }
            """;

        // GroupReadinessHandler schema (groups with readiness)
        private const string ReadinessJson = """
            {
              "groups": [
                {"id":"GRP-001","name":"Active Recruitment","islandIds":["ISL-001","ISL-002"],"whyTogether":"Both relate to recruiting","readiness":"Ready","readinessNotes":null},
                {"id":"GRP-002","name":"Support","islandIds":["ISL-003"],"whyTogether":"Academic support is distinct","readiness":"Ready","readinessNotes":null}
              ]
            }
            """;

        private const string DistillJson = """
            {
              "groupDistillations": [
                {
                  "groupId":"GRP-001",
                  "decisions":[{"id":"DEC-001","description":"Build recruitment journey first","impact":"high"}],
                  "deliverables":[{"deliverableId":"DEL-001","path":"personas/athlete.md","purpose":"Persona card","status":"Draft"}],
                  "questions":[]
                }
              ],
              "distilledIslands": [
                {"islandId":"ISL-001","newStatus":"Distilled"},
                {"islandId":"ISL-002","newStatus":"Distilled"},
                {"islandId":"ISL-003","newStatus":"Distilled"}
              ],
              "gateSatisfied": true
            }
            """;

        private const string HtmlResponse = """{"html":"<html><body>Athlete persona content</body></html>"}""";

        private const string TriageEmpty = """{"triaged":[]}""";

        private const string CheckpointContext = """{"sessionContext":"Prior checkpoint context summary"}""";

        public virtual Task<TResult> SendHandlerAsync<TResult>(
            IReadOnlyList<ChatMessage> messages,
            string jsonSchema,
            Func<JsonElement, TResult> parse,
            CancellationToken ct = default)
        {
            var userTurn   = messages.FirstOrDefault(m => m.Role == MessageRole.User)?.Content ?? "";
            var systemTurn = messages.FirstOrDefault(m => m.Role == MessageRole.System)?.Content ?? "";

            string json;
            if (jsonSchema.Contains("sessionContext"))
                json = CheckpointContext;
            else if (jsonSchema.Contains("triaged"))
                json = TriageEmpty;
            else if (jsonSchema.Contains("sessionObjective"))
                json = KickoffJson;
            else if (jsonSchema.Contains("\"islands\""))
                json = CaptureJson;
            else if (jsonSchema.Contains("ungroupedIslandIds"))
                json = GroupingJson;            // IslandGroupingHandler
            else if (jsonSchema.Contains("readiness") && jsonSchema.Contains("groups"))
                json = ReadinessJson;           // GroupReadinessHandler
            else if (jsonSchema.Contains("groupDistillations"))
                json = DistillJson;
            else if (jsonSchema.Contains("inputTokens"))
                json = ExpressReviewJson;
            else if (jsonSchema.Contains("html"))
                json = HtmlResponse;
            else
                json = """{"gateSatisfied":true}""";

            return Task.FromResult(parse(JsonDocument.Parse(json).RootElement));
        }

        public Task<StepResult> SendAsync(
            IReadOnlyList<ChatMessage> messages,
            AgentStep step,
            CancellationToken ct = default)
        {
            var json = """{"gateSatisfied":true}""";
            var doc  = JsonDocument.Parse(json);
            return Task.FromResult(step.ParseResult(doc.RootElement, json, true));
        }
    }

}
