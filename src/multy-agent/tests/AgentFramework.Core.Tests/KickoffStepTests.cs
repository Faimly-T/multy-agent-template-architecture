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

public class KickoffStepTests
{
    private const string TestDataPath = "TestData/UxAgentRole.md";

    private static KickoffStep CreateStep() =>
        new(PromptContext.Empty, 1, "Session Objective. Parse product description.", new Gate("Objective confirmed"));

    private static async Task<UxAgent> CreateAgentAsync()
    {
        return await UxAgent.BuildAsync(UxAgentDefaults.Config(TestSteps.DefaultRole()), TestSteps.DefaultResolver());
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

        var Kickoff = Assert.IsType<KickoffResult>(result);
        Assert.True(Kickoff.GateSatisfied);
        Assert.Contains("Build personas", Kickoff.SessionObjective);
        Assert.Contains("Initial session", Kickoff.NarrativeBridge);
        Assert.True(Kickoff.IsInitialSession);
        Assert.Null(Kickoff.StalenessWarning);
        Assert.Empty(Kickoff.Blockers!);
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

        var Kickoff = Assert.IsType<KickoffResult>(result);
        Assert.False(Kickoff.IsInitialSession);
        Assert.NotNull(Kickoff.StalenessWarning);
        Assert.Contains("5 days ago", Kickoff.StalenessWarning);
        Assert.Equal(2, Kickoff.Blockers!.Count);
        Assert.Equal("hard", Kickoff.Blockers[0].Severity);
        Assert.Equal("soft", Kickoff.Blockers[1].Severity);
        Assert.Equal("UX-Q003", Kickoff.Blockers[0].QuestionId);
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

        var Kickoff = Assert.IsType<KickoffResult>(result);
        Assert.Equal("Build personas", Kickoff.SessionObjective);
        Assert.Equal(string.Empty, Kickoff.NarrativeBridge);
        Assert.False(Kickoff.IsInitialSession);
        Assert.Null(Kickoff.StalenessWarning);
        Assert.Empty(Kickoff.Blockers!);
    }

    // ==========================================================
    // ApplyTo — updates session objective
    // ==========================================================

    [Fact]
    public async Task ApplyTo_UpdatesSessionObjective()
    {
        var agent = await CreateAgentAsync();
        var result = new KickoffResult(
            Output: "json output",
            GateSatisfied: true,
            SessionObjective: "Build 3 validated persona cards for athletic recruiting",
            NarrativeBridge: "First session — starting fresh.",
            IsInitialSession: true);

        result.ApplyTo(agent);

        Assert.Equal("Build 3 validated persona cards for athletic recruiting", agent.Session!.CurrentCheckpoint!.SessionObjective);
    }

    [Fact]
    public async Task ApplyTo_PreservesExistingSessionState()
    {
        var agent = await CreateAgentAsync();
        agent.Brain.Backlog.SetCaptured([
            new CapturedIsland("ISL-001", IslandType.UserType, "Athlete", "input")
        ]);
        agent.RaiseQuestion("UX-Q001", "What sport?", "express");

        var result = new KickoffResult("json", true, "Refined objective");
        result.ApplyTo(agent);

        Assert.Equal("Refined objective", agent.Session!.CurrentCheckpoint!.SessionObjective);
        Assert.Single(agent.Brain.Backlog.All);
        Assert.Single(agent.Questions);
    }

    // ==========================================================
    // KickoffBlocker record
    // ==========================================================

    [Fact]
    public void KickoffBlocker_RecordEquality()
    {
        var b1 = new KickoffBlocker("Q-001", "Missing data", "hard");
        var b2 = new KickoffBlocker("Q-001", "Missing data", "hard");

        Assert.Equal(b1, b2);
    }

    // ==========================================================
    // UxAgent agent — full first step with real skill
    // ==========================================================

    [Fact]
    public async Task UxAgent_Step1_WithRealSkill_ProducesKickoffResult()
    {
        var agent = await CreateAgentAsync();
        var client = new KickoffFakeChatClient();

        var result = await agent.ExecuteNextStepAsync(client);

        Assert.NotNull(result);
        var Kickoff = Assert.IsType<KickoffResult>(result);
        Assert.True(Kickoff.GateSatisfied);

        Assert.True(Kickoff.IsInitialSession);
        Assert.NotEmpty(Kickoff.NarrativeBridge);
    }

    [Fact]
    public async Task UxAgent_Step1_JournalIsRecorded()
    {
        var agent = await CreateAgentAsync();
        var client = new KickoffFakeChatClient();

        await agent.ExecuteNextStepAsync(client);

        Assert.NotEmpty(agent.GetStepJournal("KickoffStep"));
    }

    [Fact]
    public async Task UxAgent_Step1_JournalOutputContainsSessionObjective()
    {
        var agent = await CreateAgentAsync();
        var client = new KickoffFakeChatClient();

        await agent.ExecuteNextStepAsync(client);

        var output = agent.GetStepJournal("KickoffStep")[^1].Content.Output;
        Assert.Contains("sessionObjective", output);
    }

    [Fact]
    public async Task UxAgent_Step1_SessionUpdatedAfterExecution()
    {
        var agent = await CreateAgentAsync();
        var client = new KickoffFakeChatClient();

        await agent.ExecuteNextStepAsync(client);

        Assert.NotNull(agent.Session);
        Assert.Equal(
            "Build validated persona cards for college athletic recruiting platform. Success = 3+ distinct personas with JTBD. Stakes: journey mapping and UX design blocked without foundational personas.",
            agent.Session.CurrentCheckpoint!.SessionObjective);
        Assert.Equal(1, agent.Pipeline.CurrentStepIndex);
    }

    [Fact]
    public async Task UxAgent_Step1_WithBlockers_ParsesBlockerList()
    {
        var agent = await CreateAgentAsync();
        agent.RaiseQuestion("UX-Q003", "No access to user research data", "express");
        agent.RaiseQuestion("UX-Q004", "Anti-persona priority unclear", "express");
        var client = new KickoffFakeChatClientWithBlockers();

        var result = await agent.ExecuteNextStepAsync(client);

        var Kickoff = Assert.IsType<KickoffResult>(result);
        Assert.True(Kickoff.GateSatisfied);
        Assert.Equal(2, Kickoff.Blockers!.Count);
        Assert.Equal("hard", Kickoff.Blockers[0].Severity);
    }

    [Fact]
    public async Task UxAgent_Step1_GateFailed_DoesNotAdvanceStep()
    {
        var agent = await CreateAgentAsync();
        var client = new KickoffGateFailChatClient();

        var result = await agent.ExecuteNextStepAsync(client);

        Assert.False(result.GateSatisfied);
        Assert.Equal(0, agent.Pipeline.CurrentStepIndex);
    }

    // ==========================================================
    // Fakes
    // ==========================================================

    private class KickoffFakeChatClient : IChatClient
    {
        private const string KickoffJson = """
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
                : KickoffJson;
            return Task.FromResult(parse(JsonDocument.Parse(json).RootElement));
        }

        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            var doc = JsonDocument.Parse(KickoffJson);
            var result = step.ParseResult(doc.RootElement, KickoffJson, true);
            return Task.FromResult(result);
        }
    }

    private class KickoffFakeChatClientWithBlockers : IChatClient
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

    private class KickoffGateFailChatClient : IChatClient
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
