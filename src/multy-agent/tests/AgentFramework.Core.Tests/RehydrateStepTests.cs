using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Agent.Steps.CODESteps;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class RehydrateStepTests
{
    private const string TestDataPath = "TestData/UxPersonaRole.md";

    private static RehydrateStep CreateStep() =>
        new(1, "Define objective for agent", "Session Objective. Parse product description.", new Gate("Objective confirmed"));

    private static UxPersona CreateAgent()
    {
        var markdown = File.ReadAllText(TestDataPath);
        return new UxPersona(Role.FromMd(markdown), TestSteps.DefaultSteps());
    }

    private static UxPersona CreateAgentWithSkills()
    {
        var markdown = File.ReadAllText(TestDataPath);
        var skills = TestSteps.DefaultSteps()
            .Select(s => Skill.FromMd(File.ReadAllText($"TestData/Skills/{s.SkillName}.md")));
        var steps = UxStepBuilder.Create()
            .WithSteps(TestSteps.DefaultSteps())
            .WithSkills(skills)
            .Build();
        return new UxPersona(Role.FromMd(markdown), steps);
    }

    // ==========================================================
    // Schema
    // ==========================================================

    [Fact]
    public void Schema_ContainsSessionObjective()
    {
        var step = CreateStep();
        Assert.Contains("sessionObjective", step.JsonResponseSchema);
    }

    [Fact]
    public void Schema_ContainsNarrativeBridge()
    {
        var step = CreateStep();
        Assert.Contains("narrativeBridge", step.JsonResponseSchema);
    }

    [Fact]
    public void Schema_ContainsBlockers()
    {
        var step = CreateStep();
        Assert.Contains("blockers", step.JsonResponseSchema);
        Assert.Contains("severity", step.JsonResponseSchema);
    }

    [Fact]
    public void Schema_ContainsStalenessWarning()
    {
        var step = CreateStep();
        Assert.Contains("stalenessWarning", step.JsonResponseSchema);
    }

    [Fact]
    public void Schema_ContainsIsInitialSession()
    {
        var step = CreateStep();
        Assert.Contains("isInitialSession", step.JsonResponseSchema);
    }

    // ==========================================================
    // BuildContext — null session
    // ==========================================================

    [Fact]
    public void BuildContext_NullSession_ReturnsInitialSession()
    {
        var step = CreateStep();

        var context = step.BuildContext(null);

        Assert.Contains("Initial Session", context);
        Assert.Contains("session #1", context);
    }

    // ==========================================================
    // BuildContext — initial session (iteration 1)
    // ==========================================================

    [Fact]
    public void BuildContext_InitialSession_ContainsObjective()
    {
        var step = CreateStep();
        var session = new AgentSession("Build personas for recruiting platform");

        var context = step.BuildContext(session);

        Assert.Contains("Initial Session", context);
        Assert.Contains("Build personas for recruiting platform", context);
        Assert.Contains("verb + deliverable + success condition + stakes", context);
    }

    [Fact]
    public void BuildContext_InitialSession_NoStalenessWarning()
    {
        var step = CreateStep();
        var session = new AgentSession("Build personas");

        var context = step.BuildContext(session);

        Assert.DoesNotContain("Staleness warning", context);
    }

    [Fact]
    public void BuildContext_InitialSession_NoOpenQuestions_OmitsQuestionsBlock()
    {
        var step = CreateStep();
        var session = new AgentSession("Build personas");

        var context = step.BuildContext(session);

        Assert.DoesNotContain("Open questions", context);
        Assert.DoesNotContain("Answered questions", context);
    }

    // ==========================================================
    // BuildContext — continuation session (iteration > 1)
    // ==========================================================

    [Fact]
    public void BuildContext_ContinuationSession_ContainsIteration()
    {
        var step = CreateStep();
        var session = new AgentSession("Refine personas", sessionIteration: 2);

        var context = step.BuildContext(session);

        Assert.Contains("Session iteration: 2", context);
        Assert.Contains("Refine personas", context);
        Assert.DoesNotContain("Initial Session", context);
    }

    [Fact]
    public void BuildContext_ContinuationSession_ContainsCheckpointDate()
    {
        var step = CreateStep();
        var session = new AgentSession("Refine personas", sessionIteration: 2);

        var context = step.BuildContext(session);

        Assert.Contains("Prior checkpoint date:", context);
        Assert.Contains(DateTime.UtcNow.ToString("yyyy-MM-dd"), context);
    }

    // ==========================================================
    // BuildContext — answered questions
    // ==========================================================

    [Fact]
    public void BuildContext_WithAnsweredQuestions_IncludesAnswerBlock()
    {
        var step = CreateStep();
        var session = new AgentSession("Build personas");
        session.RaiseQuestion("UX-Q001", "What sport?", "express");
        session.FindQuestion("UX-Q001")!.SetAnswer("Football", "PjM Interview");

        var context = step.BuildContext(session);

        Assert.Contains("Answered questions from prior session", context);
        Assert.Contains("UX-Q001", context);
        Assert.Contains("Football", context);
        Assert.Contains("PjM Interview", context);
    }

    [Fact]
    public void BuildContext_WithAnsweredQuestions_InstructsExpressReview()
    {
        var step = CreateStep();
        var session = new AgentSession("Build personas");
        session.RaiseQuestion("UX-Q001", "What sport?", "express");
        session.FindQuestion("UX-Q001")!.SetAnswer("Football", "PjM");

        var context = step.BuildContext(session);

        Assert.Contains("Express step", context);
        Assert.Contains("reviewed list", context);
    }

    // ==========================================================
    // BuildContext — open questions
    // ==========================================================

    [Fact]
    public void BuildContext_WithOpenQuestions_IncludesTriageBlock()
    {
        var step = CreateStep();
        var session = new AgentSession("Build personas");
        session.RaiseQuestion("UX-Q002", "What's the target market?", "express");

        var context = step.BuildContext(session);

        Assert.Contains("Open questions from prior session", context);
        Assert.Contains("triage each", context);
        Assert.Contains("UX-Q002", context);
        Assert.Contains("What's the target market?", context);
    }

    [Fact]
    public void BuildContext_MixedQuestionStatuses_ShowsBothBlocks()
    {
        var step = CreateStep();
        var session = new AgentSession("Build personas");
        session.RaiseQuestion("UX-Q001", "What sport?", "express");
        session.FindQuestion("UX-Q001")!.SetAnswer("Football", "PjM");
        session.RaiseQuestion("UX-Q002", "What market?", "express");

        var context = step.BuildContext(session);

        Assert.Contains("Answered questions from prior session", context);
        Assert.Contains("Open questions from prior session", context);
        Assert.Contains("UX-Q001", context);
        Assert.Contains("UX-Q002", context);
    }

    // ==========================================================
    // BuildContext — prior work summary
    // ==========================================================

    [Fact]
    public void BuildContext_WithPriorWork_IncludesSummary()
    {
        var step = CreateStep();
        var session = new AgentSession("Refine personas", sessionIteration: 2);
        session.CaptureIsland("ISL-001", IslandType.UserType, "Student athlete", "input");
        session.RecordDecision("DEC-001", "Merge clusters", "Cleaner model");
        session.RecordDeliverable("DEL-001", "outputs/personas/01.md", DeliverableStatus.Complete);

        var context = step.BuildContext(session);

        Assert.Contains("Prior work summary", context);
        Assert.Contains("Islands: 1 total", context);
        Assert.Contains("Decisions: 1", context);
        Assert.Contains("Deliverables: 1", context);
    }

    [Fact]
    public void BuildContext_NoPriorWork_OmitsSummary()
    {
        var step = CreateStep();
        var session = new AgentSession("Build personas");

        var context = step.BuildContext(session);

        Assert.DoesNotContain("Prior work summary", context);
    }

    // ==========================================================
    // ParseResult — full JSON
    // ==========================================================

    [Fact]
    public void ParseResult_FullJson_ExtractsAllFields()
    {
        var step = CreateStep();
        var json = """
            {
              "sessionObjective": "Build personas for recruiting platform. Success = 3+ validated persona cards. Stakes: downstream journey mapping blocked without personas.",
              "narrativeBridge": "Initial session — no prior context. Starting from product description.",
              "isInitialSession": true,
              "stalenessWarning": null,
              "blockers": [],
              "gateSatisfied": true
            }
            """;

        var doc = JsonDocument.Parse(json);
        var result = step.ParseResult(doc.RootElement, json, true);

        var rehydrate = Assert.IsType<RehydrateResult>(result);
        Assert.True(rehydrate.GateSatisfied);
        Assert.Contains("Build personas", rehydrate.SessionObjective);
        Assert.Contains("Initial session", rehydrate.NarrativeBridge);
        Assert.True(rehydrate.IsInitialSession);
        Assert.Null(rehydrate.StalenessWarning);
        Assert.Empty(rehydrate.Blockers!);
    }

    [Fact]
    public void ParseResult_WithBlockers_ExtractsBlockerList()
    {
        var step = CreateStep();
        var json = """
            {
              "sessionObjective": "Refine personas with answered questions",
              "narrativeBridge": "Continuing from session 1 where 3 personas were drafted.",
              "isInitialSession": false,
              "stalenessWarning": "Last checkpoint was 5 days ago — priorities may have shifted.",
              "blockers": [
                { "questionId": "UX-Q003", "text": "No access to user research data", "severity": "hard" },
                { "questionId": "UX-Q004", "text": "Unsure about anti-persona priority", "severity": "soft" }
              ],
              "gateSatisfied": true
            }
            """;

        var doc = JsonDocument.Parse(json);
        var result = step.ParseResult(doc.RootElement, json, true);

        var rehydrate = Assert.IsType<RehydrateResult>(result);
        Assert.False(rehydrate.IsInitialSession);
        Assert.NotNull(rehydrate.StalenessWarning);
        Assert.Contains("5 days ago", rehydrate.StalenessWarning);
        Assert.Equal(2, rehydrate.Blockers!.Count);
        Assert.Equal("hard", rehydrate.Blockers[0].Severity);
        Assert.Equal("soft", rehydrate.Blockers[1].Severity);
        Assert.Equal("UX-Q003", rehydrate.Blockers[0].QuestionId);
    }

    [Fact]
    public void ParseResult_MinimalJson_DefaultsOptionalFields()
    {
        var step = CreateStep();
        var json = """
            {
              "sessionObjective": "Build personas",
              "gateSatisfied": true
            }
            """;

        var doc = JsonDocument.Parse(json);
        var result = step.ParseResult(doc.RootElement, json, true);

        var rehydrate = Assert.IsType<RehydrateResult>(result);
        Assert.Equal("Build personas", rehydrate.SessionObjective);
        Assert.Equal(string.Empty, rehydrate.NarrativeBridge);
        Assert.False(rehydrate.IsInitialSession);
        Assert.Null(rehydrate.StalenessWarning);
        Assert.Empty(rehydrate.Blockers!);
    }

    // ==========================================================
    // ApplyTo — updates session objective
    // ==========================================================

    [Fact]
    public void ApplyTo_UpdatesSessionObjective()
    {
        var session = new AgentSession("placeholder");
        var result = new RehydrateResult(
            Output: "json output",
            GateSatisfied: true,
            SessionObjective: "Build 3 validated persona cards for athletic recruiting",
            NarrativeBridge: "First session — starting fresh.",
            IsInitialSession: true);

        result.ApplyTo(session);

        Assert.Equal("Build 3 validated persona cards for athletic recruiting", session.Checkpoint.SessionObjective);
    }

    [Fact]
    public void ApplyTo_PreservesExistingSessionState()
    {
        var session = new AgentSession("initial", sessionIteration: 2);
        session.CaptureIsland("ISL-001", IslandType.UserType, "Athlete", "input");
        session.RaiseQuestion("UX-Q001", "What sport?", "express");

        var result = new RehydrateResult("json", true, "Refined objective");
        result.ApplyTo(session);

        Assert.Equal("Refined objective", session.Checkpoint.SessionObjective);
        Assert.Single(session.Islands);
        Assert.Single(session.Questions);
    }

    // ==========================================================
    // RehydrateBlocker record
    // ==========================================================

    [Fact]
    public void RehydrateBlocker_RecordEquality()
    {
        var b1 = new RehydrateBlocker("Q-001", "Missing data", "hard");
        var b2 = new RehydrateBlocker("Q-001", "Missing data", "hard");

        Assert.Equal(b1, b2);
    }

    // ==========================================================
    // UxPersona agent — full first step with real skill
    // ==========================================================

    [Fact]
    public async Task UxPersona_Step1_WithRealSkill_ProducesRehydrateResult()
    {
        var agent = CreateAgentWithSkills();
        agent.StartSession("Analyze a college athletic recruiting platform that connects high-school athletes with university scouts.");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateFakeChatClient();

        var result = await agent.ExecuteNextStepAsync(builder, client);

        Assert.NotNull(result);
        var rehydrate = Assert.IsType<RehydrateResult>(result);
        Assert.True(rehydrate.GateSatisfied);
        Assert.NotEmpty(rehydrate.SessionObjective);
        Assert.True(rehydrate.IsInitialSession);
        Assert.NotEmpty(rehydrate.NarrativeBridge);
    }

    [Fact]
    public async Task UxPersona_Step1_MessageContainsSkillInstructions()
    {
        var agent = CreateAgentWithSkills();
        agent.StartSession("Analyze athletic recruiting platform");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateFakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        var userMsg = agent.ConversationMessages.First(m => m.Role == MessageRole.User);
        Assert.Contains("Skill: rehydrate-context", userMsg.Content);
        Assert.Contains("session checkpoint", userMsg.Content);
        Assert.Contains("Synthesize Session Objective", userMsg.Content);
        Assert.Contains("verb + deliverable + success condition + stakes", userMsg.Content);
    }

    [Fact]
    public async Task UxPersona_Step1_MessageContainsJsonSchema()
    {
        var agent = CreateAgentWithSkills();
        agent.StartSession("Analyze athletic recruiting platform");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateFakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        var userMsg = agent.ConversationMessages.First(m => m.Role == MessageRole.User);
        Assert.Contains("sessionObjective", userMsg.Content);
        Assert.Contains("narrativeBridge", userMsg.Content);
        Assert.Contains("blockers", userMsg.Content);
        Assert.Contains("stalenessWarning", userMsg.Content);
        Assert.Contains("Required Response Format", userMsg.Content);
    }

    [Fact]
    public async Task UxPersona_Step1_MessageContainsInitialSessionContext()
    {
        var agent = CreateAgentWithSkills();
        agent.StartSession("Build personas for athletic recruiting");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateFakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        var userMsg = agent.ConversationMessages.First(m => m.Role == MessageRole.User);
        Assert.Contains("Initial Session", userMsg.Content);
        Assert.Contains("Build personas for athletic recruiting", userMsg.Content);
    }

    [Fact]
    public async Task UxPersona_Step1_SessionUpdatedAfterExecution()
    {
        var agent = CreateAgentWithSkills();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateFakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        Assert.NotNull(agent.Session);
        Assert.Equal(
            "Build validated persona cards for college athletic recruiting platform. Success = 3+ distinct personas with JTBD. Stakes: journey mapping and UX design blocked without foundational personas.",
            agent.Session.Checkpoint.SessionObjective);
        Assert.Equal(1, agent.CurrentStepIndex);
    }

    [Fact]
    public async Task UxPersona_Step1_ConversationHasSystemUserAssistant()
    {
        var agent = CreateAgentWithSkills();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateFakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        Assert.True(agent.ConversationMessages.Count >= 3);
        Assert.Equal(MessageRole.System, agent.ConversationMessages[0].Role);
        Assert.Equal(MessageRole.User, agent.ConversationMessages[1].Role);
        Assert.Equal(MessageRole.Assistant, agent.ConversationMessages[2].Role);
    }

    [Fact]
    public async Task UxPersona_Step1_SystemPromptContainsRoleIdentity()
    {
        var agent = CreateAgentWithSkills();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateFakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        var sysMsg = agent.ConversationMessages.First(m => m.Role == MessageRole.System);
        Assert.Contains("Clara Mendes", sysMsg.Content);
        Assert.Contains("Senior UX Researcher", sysMsg.Content);
        Assert.Contains("valid JSON", sysMsg.Content);
    }

    [Fact]
    public async Task UxPersona_Step1_RaisesStepEvents()
    {
        var agent = CreateAgentWithSkills();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateFakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        Assert.Equal(2, agent.DomainEvents.Count);
        Assert.IsType<AgentFramework.Core.Agent.Events.StepStarted>(agent.DomainEvents[0]);
        Assert.IsType<AgentFramework.Core.Agent.Events.StepCompleted>(agent.DomainEvents[1]);
    }

    [Fact]
    public async Task UxPersona_Step1_WithBlockers_ParsesBlockerList()
    {
        var agent = CreateAgentWithSkills();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateFakeChatClientWithBlockers();

        var result = await agent.ExecuteNextStepAsync(builder, client);

        var rehydrate = Assert.IsType<RehydrateResult>(result);
        Assert.True(rehydrate.GateSatisfied);
        Assert.Equal(2, rehydrate.Blockers!.Count);
        Assert.Equal("hard", rehydrate.Blockers[0].Severity);
    }

    [Fact]
    public async Task UxPersona_Step1_GateFailed_DoesNotAdvanceStep()
    {
        var agent = CreateAgentWithSkills();
        agent.StartSession("placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateGateFailChatClient();

        var result = await agent.ExecuteNextStepAsync(builder, client);

        Assert.False(result.GateSatisfied);
        Assert.Equal(0, agent.CurrentStepIndex);
    }

    // ==========================================================
    // Fakes
    // ==========================================================

    private class RehydrateFakeChatClient : IChatClient
    {
        private const string RehydrateJson = """
            {
              "sessionObjective": "Build validated persona cards for college athletic recruiting platform. Success = 3+ distinct personas with JTBD. Stakes: journey mapping and UX design blocked without foundational personas.",
              "narrativeBridge": "Initial session — no prior context exists. Starting from the product description to identify user types, goals, and pain points.",
              "isInitialSession": true,
              "stalenessWarning": null,
              "blockers": [],
              "gateSatisfied": true
            }
            """;

        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            var doc = JsonDocument.Parse(RehydrateJson);
            var result = step.ParseResult(doc.RootElement, RehydrateJson, true);
            return Task.FromResult(result);
        }
    }

    private class RehydrateFakeChatClientWithBlockers : IChatClient
    {
        private const string RehydrateJson = """
            {
              "sessionObjective": "Refine persona cards for recruiting platform. Success = address all blockers. Stakes: personas remain incomplete.",
              "narrativeBridge": "Continuing from session 1 — 3 draft personas were created but blockers remain.",
              "isInitialSession": false,
              "stalenessWarning": "Last checkpoint was 5 days ago — priorities may have shifted.",
              "blockers": [
                { "questionId": "UX-Q003", "text": "No access to user research data", "severity": "hard" },
                { "questionId": "UX-Q004", "text": "Anti-persona priority unclear", "severity": "soft" }
              ],
              "gateSatisfied": true
            }
            """;

        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            var doc = JsonDocument.Parse(RehydrateJson);
            var result = step.ParseResult(doc.RootElement, RehydrateJson, true);
            return Task.FromResult(result);
        }
    }

    private class RehydrateGateFailChatClient : IChatClient
    {
        private const string RehydrateJson = """
            {
              "sessionObjective": "",
              "gateSatisfied": false
            }
            """;

        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            var doc = JsonDocument.Parse(RehydrateJson);
            var result = step.ParseResult(doc.RootElement, RehydrateJson, false);
            return Task.FromResult(result);
        }
    }
}
