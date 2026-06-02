using System.Text.Json;
using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.CodePipeline;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class SkillConversationTests
{
    private const string TestDataPath = "TestData/UxAgentRole.md";

    private static async Task<UxAgent> CreateAgentAsync()
    {
        return await UxAgent.BuildAsync(UxAgentDefaults.Config(TestSteps.DefaultRole()), TestSteps.DefaultResolver());
    }

    // --- Skill loading ---

    [Fact]
    public void Skill_FromMd_ParsesNameAndDescription()
    {
        var md = File.ReadAllText("TestData/Skills/Kickoff-context.md");

        var skill = SkillParser.ParseFromMarkdown(md);

        Assert.Equal("Kickoff-context", skill.Name);
        Assert.Equal("Define objective for agent and reconstruct session from prior state.", skill.Description);
        Assert.Contains("session checkpoint", skill.Content);
    }

    // --- Step journals contain the LLM output ---

    [Fact]
    public async Task Step1_JournalOutputContainsKickoffJson()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);

        var output = agent.GetStepJournal("KickoffStep")[^1].Content.Output;
        Assert.Contains("sessionObjective", output);
        Assert.Contains("gateSatisfied", output);
    }

    [Fact]
    public async Task Step2_JournalOutputContainsIslandsJson()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client); // step 1
        await agent.ExecuteNextStepAsync(client); // step 2

        var output = agent.GetStepJournal("CaptureStep")[^1].Content.Output;
        Assert.Contains("islands", output);
        Assert.Contains("relatesToIslandId", output);
    }

    [Fact]
    public async Task Step3_JournalOutputContainsOrganizeJson()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client);
        await agent.ExecuteNextStepAsync(client);
        await agent.ExecuteNextStepAsync(client);

        var output = agent.GetStepJournal("OrganizeStep")[^1].Content.Output;
        Assert.Contains("organizedIslands", output);
        Assert.Contains("groups", output);
    }

    [Fact]
    public async Task Step2_JournalOutputContainsGateSatisfied()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(client); // step 1
        await agent.ExecuteNextStepAsync(client); // step 2

        var output = agent.GetStepJournal("CaptureStep")[^1].Content.Output;
        Assert.Contains("islands", output);
        Assert.Contains("gateSatisfied", output);
    }

    // --- Full pipeline ---

    [Fact]
    public async Task FullPipeline_WithSkills_MapsAllStepsToSession()
    {
        var agent = await CreateAgentAsync();
        var client = new FakeChatClient();

        var results = await agent.ExecuteAllStepsAsync(TestSteps.DefaultIntent, client);

        Assert.Equal(6, results.Count);
        Assert.True(agent.IsCompleted);
        Assert.Equal("Build personas for college athletic recruiting platform", agent.Session!.CurrentCheckpoint!.SessionObjective);
        Assert.Equal(3, agent.Session.Islands.Count);
        Assert.Single(agent.Decisions);
        Assert.Single(agent.Deliverables);
        Assert.Equal(7000, agent.Session.CurrentCheckpoint!.TokensConsumption.TotalTokens);
    }

    // --- Fakes ---

    private class FakeChatClient : IChatClient
    {
        public Task<TResult> SendHandlerAsync<TResult>(
            IReadOnlyList<ChatMessage> messages, string jsonSchema,
            Func<JsonElement, TResult> parse, CancellationToken ct = default)
        {
            string json;
            if (jsonSchema.Contains("triaged"))
                json = """{"triaged":[]}""";
            else if (jsonSchema.Contains("groupDistillations"))
                json = """{"groupDistillations":[{"groupId":"GRP-001","decisions":[{"id":"DEC-001","description":"Merge pain into athlete","impact":"Cleaner model"}],"deliverables":[{"deliverableId":"DEL-001","path":"outputs/personas/01-athlete.md","purpose":"Athlete persona card","status":"Complete"}],"questions":[]}],"distilledIslands":[{"islandId":"ISL-001","newStatus":"Distilled"},{"islandId":"ISL-002","newStatus":"Distilled"}],"gateSatisfied":true}""";
            else if (jsonSchema.Contains("readiness"))
                json = """{"groups":[{"id":"GRP-001","name":"Recruiting Group","islandIds":["ISL-001","ISL-002"],"whyTogether":"Both describe recruiting","readiness":"Ready","readinessNotes":null}]}""";
            else if (jsonSchema.Contains("islandIds"))
                json = """{"groups":[{"id":"GRP-001","name":"Recruiting Group","islandIds":["ISL-001","ISL-002"],"whyTogether":"Both describe recruiting"}],"ungroupedIslandIds":["ISL-003"]}""";
            else if (jsonSchema.Contains("islands"))
                json = """{"islands":[{"id":"ISL-001","type":"UserType","description":"Student athlete seeking recruitment","source":"six-hats:yellow","relatesToIslandId":null},{"id":"ISL-002","type":"Stakeholder","description":"College coach evaluating talent","source":"six-hats:white","relatesToIslandId":null},{"id":"ISL-003","type":"PainPoint","description":"No visibility into recruiting process","source":"six-hats:black","relatesToIslandId":"ISL-001"}]}""";
            else if (jsonSchema.Contains("\"html\""))
                json = """{"html":"<html><body>Test Document</body></html>"}""";
            else if (jsonSchema.Contains("inputTokens"))
                json = """{"questions":[],"inputTokens":2000,"outputTokens":5000,"gateSatisfied":true}""";
            else
                json = """{"sessionObjective":"Build personas for college athletic recruiting platform","narrativeBridge":"Initial session — no prior context.","isInitialSession":true,"stalenessWarning":null,"gateSatisfied":true}""";
            return Task.FromResult(parse(JsonDocument.Parse(json).RootElement));
        }

        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            StepResult result = step.StepNumber switch
            {
                1 => new KickoffResult(
                    Output: """{"sessionObjective":"Build personas for college athletic recruiting platform","narrativeBridge":"Initial session — no prior context.","isInitialSession":true,"stalenessWarning":null,"blockers":[],"gateSatisfied":true}""",
                    GateSatisfied: true,
                    SessionObjective: "Build personas for college athletic recruiting platform",
                    NarrativeBridge: "Initial session — no prior context.",
                    IsInitialSession: true),

                2 => new CaptureResult(
                    Output: """{"islands":[{"id":"ISL-001","type":"UserType","description":"Student athlete","source":"product desc"},{"id":"ISL-002","type":"Stakeholder","description":"College coach","source":"product desc"},{"id":"ISL-003","type":"PainPoint","description":"No visibility","source":"interview","relatesToIslandId":"ISL-001"}],"gateSatisfied":true}""",
                    GateSatisfied: true,
                    Islands:
                    [
                        new("ISL-001", IslandType.UserType, "Student athlete", "product desc"),
                        new("ISL-002", IslandType.Stakeholder, "College coach", "product desc"),
                        new("ISL-003", IslandType.PainPoint, "No visibility", "interview", "ISL-001"),
                    ]),

                3 => new OrganizeResult(
                    Output: """{"organizedIslands":[{"islandId":"ISL-001","newStatus":"Organized"},{"islandId":"ISL-002","newStatus":"Organized"},{"islandId":"ISL-003","newStatus":"Discarded"}],"decisions":[{"id":"DEC-001","description":"Merge pain into athlete","impact":"Cleaner model"}],"gateSatisfied":true}""",
                    GateSatisfied: true,
                    OrganizedIslands:
                    [
                        new("ISL-001", IslandStatus.Organized),
                        new("ISL-002", IslandStatus.Organized),
                        new("ISL-003", IslandStatus.Discarded),
                    ],
                    Decisions: [new("DEC-001", "Merge pain into athlete", "Cleaner model")]),

                4 => new DistillResult(
                    Output: """{"distilledIslands":[{"islandId":"ISL-001","newStatus":"Distilled"},{"islandId":"ISL-002","newStatus":"Distilled"}],"deliverables":[{"deliverableId":"DEL-001","path":"outputs/personas/01-athlete.md","status":"Complete"}],"gateSatisfied":true}""",
                    GateSatisfied: true,
                    DistilledIslands:
                    [
                        new("ISL-001", IslandStatus.Distilled),
                        new("ISL-002", IslandStatus.Distilled),
                    ],
                    Deliverables: [new("DEL-001", "outputs/personas/01-athlete.md", DeliverableStatus.Complete)]),

                5 => new ExpressResult(
                    Output: """{"inputTokens":2000,"outputTokens":5000,"gateSatisfied":true}""",
                    GateSatisfied: true,
                    InputTokens: 2000,
                    OutputTokens: 5000,
                    Questions: []),

                _ => new StepResult($"Step {step.StepNumber}", true),
            };

            return Task.FromResult(result);
        }
    }
}
