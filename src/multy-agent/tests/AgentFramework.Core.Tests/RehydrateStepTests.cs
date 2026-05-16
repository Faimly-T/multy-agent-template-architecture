using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps.CODESteps;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class RehydrateStepTests
{
    private const string TestDataPath = "TestData/UxPersonaRole.md";
    private static readonly SessionMarkFilePaths TestMarkFilePaths = new("UX", "outputs/contextAgent");

    private static RehydrateStep CreateStep() =>
        new(1, "Define objective for agent", "Session Objective. Parse product description.", new Gate("Objective confirmed"));

    private static UxPersona CreateAgent()
    {
        var markdown = File.ReadAllText(TestDataPath);
        return new UxPersona(RoleParser.ParseFromMarkdown(markdown), TestSteps.DefaultSteps(), TestSteps.DefaultSkills());
    }

    private static UxPersona CreateAgentWithSkills()
    {
        var markdown = File.ReadAllText(TestDataPath);
        return new UxPersona(RoleParser.ParseFromMarkdown(markdown), TestSteps.DefaultSteps(), TestSteps.DefaultSkills());
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
        var agent = CreateAgent();
        agent.OpenSession("test-proj", TestMarkFilePaths, "placeholder");
        var result = new RehydrateResult(
            Output: "json output",
            GateSatisfied: true,
            SessionObjective: "Build 3 validated persona cards for athletic recruiting",
            NarrativeBridge: "First session — starting fresh.",
            IsInitialSession: true);

        result.ApplyTo(agent);

        Assert.Equal("Build 3 validated persona cards for athletic recruiting", agent.Session!.CurrentCheckpoint!.SessionObjective);
    }

    [Fact]
    public void ApplyTo_PreservesExistingSessionState()
    {
        var agent = CreateAgent();
        agent.OpenSession("test-proj", TestMarkFilePaths, "initial");
        agent.Session!.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "input")
        ]);
        agent.RaiseQuestion("UX-Q001", "What sport?", "express");

        var result = new RehydrateResult("json", true, "Refined objective");
        result.ApplyTo(agent);

        Assert.Equal("Refined objective", agent.Session!.CurrentCheckpoint!.SessionObjective);
        Assert.Single(agent.Session.Backlog.All);
        Assert.Single(agent.Questions);
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
        agent.OpenSession("test-proj", TestMarkFilePaths, "Analyze a college athletic recruiting platform that connects high-school athletes with university scouts.");
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
        // Chain adds a minimal step-tracking user message; skill instructions are in internal handler calls
        var agent = CreateAgentWithSkills();
        agent.OpenSession("test-proj", TestMarkFilePaths, "Analyze athletic recruiting platform");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateFakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        var userMsg = agent.ConversationMessages.First(m => m.Role == MessageRole.User);
        Assert.Contains("Step 1", userMsg.Content);
        Assert.Contains("Define objective for agent", userMsg.Content);
    }

    [Fact]
    public async Task UxPersona_Step1_MessageContainsJsonSchema()
    {
        // JSON schema is in internal ObjectiveSynthesisHandler calls; assistant message has synthesis result
        var agent = CreateAgentWithSkills();
        agent.OpenSession("test-proj", TestMarkFilePaths, "Analyze athletic recruiting platform");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateFakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        var assistantMsg = agent.ConversationMessages.First(m => m.Role == MessageRole.Assistant);
        Assert.Contains("sessionObjective", assistantMsg.Content);
    }

    [Fact]
    public async Task UxPersona_Step1_MessageContainsInitialSessionContext()
    {
        // Session context is synthesized by chain handlers; conversation has step tracking only
        var agent = CreateAgentWithSkills();
        agent.OpenSession("test-proj", TestMarkFilePaths, "Build personas for athletic recruiting");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateFakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        var userMsg = agent.ConversationMessages.First(m => m.Role == MessageRole.User);
        Assert.Contains("Step 1", userMsg.Content);
    }

    [Fact]
    public async Task UxPersona_Step1_SessionUpdatedAfterExecution()
    {
        var agent = CreateAgentWithSkills();
        agent.OpenSession("test-proj", TestMarkFilePaths, "placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateFakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        Assert.NotNull(agent.Session);
        Assert.Equal(
            "Build validated persona cards for college athletic recruiting platform. Success = 3+ distinct personas with JTBD. Stakes: journey mapping and UX design blocked without foundational personas.",
            agent.Session.CurrentCheckpoint!.SessionObjective);
        Assert.Equal(1, agent.Pipeline.CurrentStepIndex);
    }

    [Fact]
    public async Task UxPersona_Step1_ConversationHasSystemUserAssistant()
    {
        var agent = CreateAgentWithSkills();
        agent.OpenSession("test-proj", TestMarkFilePaths, "placeholder");
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
        agent.OpenSession("test-proj", TestMarkFilePaths, "placeholder");
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
        agent.OpenSession("test-proj", TestMarkFilePaths, "placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateFakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        Assert.Equal(6, agent.DomainEvents.Count); // StepStarted + 4×HandlerExchanged + StepCompleted
        Assert.IsType<StepStarted>(agent.DomainEvents[0]);
        Assert.Equal(4, agent.DomainEvents.OfType<HandlerExchanged>().Count());
        Assert.IsType<StepCompleted>(agent.DomainEvents[5]);
    }

    [Fact]
    public async Task UxPersona_Step1_WithBlockers_ParsesBlockerList()
    {
        var agent = CreateAgentWithSkills();
        agent.OpenSession("test-proj", TestMarkFilePaths, "placeholder");
        agent.RaiseQuestion("UX-Q003", "No access to user research data", "express");
        agent.RaiseQuestion("UX-Q004", "Anti-persona priority unclear", "express");
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
        agent.OpenSession("test-proj", TestMarkFilePaths, "placeholder");
        var builder = new UxStepMessageBuilder();
        var client = new RehydrateGateFailChatClient();

        var result = await agent.ExecuteNextStepAsync(builder, client);

        Assert.False(result.GateSatisfied);
        Assert.Equal(0, agent.Pipeline.CurrentStepIndex);
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
              "gateSatisfied": true
            }
            """;

        public Task<TResult> SendHandlerAsync<TResult>(
            IReadOnlyList<ChatMessage> messages, string jsonSchema,
            Func<JsonElement, TResult> parse, CancellationToken ct = default)
        {
            var json = jsonSchema.Contains("triaged")
                ? """{"triaged":[]}"""
                : RehydrateJson;
            return Task.FromResult(parse(JsonDocument.Parse(json).RootElement));
        }

        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            var doc = JsonDocument.Parse(RehydrateJson);
            var result = step.ParseResult(doc.RootElement, RehydrateJson, true);
            return Task.FromResult(result);
        }
    }

    private class RehydrateFakeChatClientWithBlockers : IChatClient
    {
        private const string SynthesisJson = """
            {
              "sessionObjective": "Refine persona cards for recruiting platform. Success = address all blockers. Stakes: personas remain incomplete.",
              "narrativeBridge": "Continuing — blockers remain unresolved.",
              "isInitialSession": true,
              "stalenessWarning": null,
              "blockers": [
                { "questionId": "UX-Q003", "text": "No access to user research data", "severity": "hard" },
                { "questionId": "UX-Q004", "text": "Anti-persona priority unclear", "severity": "soft" }
              ],
              "gateSatisfied": true
            }
            """;

        public Task<TResult> SendHandlerAsync<TResult>(
            IReadOnlyList<ChatMessage> messages, string jsonSchema,
            Func<JsonElement, TResult> parse, CancellationToken ct = default)
        {
            string json;
            if (jsonSchema.Contains("triaged"))
                json = """{"triaged":[{"id":"UX-Q003","status":"still_open","answer":null,"blockerSeverity":"hard"},{"id":"UX-Q004","status":"still_open","answer":null,"blockerSeverity":"soft"}]}""";
            else
                json = SynthesisJson;
            return Task.FromResult(parse(JsonDocument.Parse(json).RootElement));
        }

        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            var doc = JsonDocument.Parse(SynthesisJson);
            var result = step.ParseResult(doc.RootElement, SynthesisJson, true);
            return Task.FromResult(result);
        }
    }

    private class RehydrateGateFailChatClient : IChatClient
    {
        private const string FailJson = """
            {
              "sessionObjective": "",
              "narrativeBridge": "",
              "isInitialSession": true,
              "stalenessWarning": null,
              "gateSatisfied": false
            }
            """;

        public Task<TResult> SendHandlerAsync<TResult>(
            IReadOnlyList<ChatMessage> messages, string jsonSchema,
            Func<JsonElement, TResult> parse, CancellationToken ct = default)
        {
            var json = jsonSchema.Contains("triaged")
                ? """{"triaged":[]}"""
                : FailJson;
            return Task.FromResult(parse(JsonDocument.Parse(json).RootElement));
        }

        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            var doc = JsonDocument.Parse(FailJson);
            var result = step.ParseResult(doc.RootElement, FailJson, false);
            return Task.FromResult(result);
        }
    }
}
