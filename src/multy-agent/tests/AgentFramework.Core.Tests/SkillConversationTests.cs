using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class SkillConversationTests
{
    private const string TestDataPath = "TestData/UxPersonaRole.md";

    private static UxPersona CreateAgent()
    {
        var markdown = File.ReadAllText(TestDataPath);
        return UxPersonaFactory.Create(markdown);
    }

    // --- Skill loading ---

    [Fact]
    public void Skill_FromMd_ParsesNameAndDescription()
    {
        var md = """
            ---
            name: rehydrate-context
            description: Define objective for agent and reconstruct session from MARK files.
            ---

            # Steps
            1. Read the Progress Summary MARK.
            2. Synthesize objective.
            """;

        var skill = Skill.FromMd(md);

        Assert.Equal("rehydrate-context", skill.Name);
        Assert.Equal("Define objective for agent and reconstruct session from MARK files.", skill.Description);
        Assert.Contains("Read the Progress Summary MARK", skill.Instructions);
    }

    [Fact]
    public async Task LoadSkills_AttachesSkillToEachStep()
    {
        var agent = CreateAgent();
        var provider = new FakeSkillProvider();

        await agent.LoadSkillsAsync(provider);

        Assert.All(agent.Steps, step => Assert.NotNull(step.Skill));
        Assert.Equal("rehydrate-context", agent.Steps[0].Skill!.Name);
        Assert.Equal("autonomous-capture", agent.Steps[1].Skill!.Name);
        Assert.Equal("strategic-organize", agent.Steps[2].Skill!.Name);
        Assert.Equal("expert-distill", agent.Steps[3].Skill!.Name);
        Assert.Equal("express-relay", agent.Steps[4].Skill!.Name);
    }

    [Fact]
    public async Task LoadSkills_PreservesStepNumbersAndGates()
    {
        var agent = CreateAgent();
        await agent.LoadSkillsAsync(new FakeSkillProvider());

        Assert.Equal(5, agent.Steps.Count);
        Assert.Equal(1, agent.Steps[0].StepNumber);
        Assert.Equal("Objective confirmed", agent.Steps[0].Gate.Description);
    }

    // --- JSON schema in messages ---

    [Fact]
    public async Task Step1_MessageContainsJsonSchema()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        await agent.LoadSkillsAsync(new FakeSkillProvider());
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        var userMsg = agent.Conversation.Messages.First(m => m.Role == MessageRole.User);
        Assert.Contains("sessionObjective", userMsg.Content);
        Assert.Contains("gateSatisfied", userMsg.Content);
        Assert.Contains("Required Response Format", userMsg.Content);
    }

    [Fact]
    public async Task Step2_MessageContainsIslandsSchema()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        await agent.LoadSkillsAsync(new FakeSkillProvider());
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client); // step 1
        await agent.ExecuteNextStepAsync(builder, client); // step 2

        var step2Msg = agent.Conversation.Messages
            .Where(m => m.Role == MessageRole.User)
            .Skip(1).First();
        Assert.Contains("islands", step2Msg.Content);
        Assert.Contains("relatesToIslandId", step2Msg.Content);
    }

    [Fact]
    public async Task Step3_MessageContainsOrganizeSchema()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        await agent.LoadSkillsAsync(new FakeSkillProvider());
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);

        var step3Msg = agent.Conversation.Messages
            .Where(m => m.Role == MessageRole.User)
            .Skip(2).First();
        Assert.Contains("organizedIslands", step3Msg.Content);
        Assert.Contains("decisions", step3Msg.Content);
    }

    // --- Skill instructions in messages ---

    [Fact]
    public async Task Step1_MessageContainsSkillInstructions()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        await agent.LoadSkillsAsync(new FakeSkillProvider());
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        var userMsg = agent.Conversation.Messages.First(m => m.Role == MessageRole.User);
        Assert.Contains("Skill: rehydrate-context", userMsg.Content);
        Assert.Contains("Read the Progress Summary MARK", userMsg.Content);
    }

    [Fact]
    public async Task Step2_MessageContainsCaptureSkill()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        await agent.LoadSkillsAsync(new FakeSkillProvider());
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);

        var step2Msg = agent.Conversation.Messages
            .Where(m => m.Role == MessageRole.User)
            .Skip(1).First();
        Assert.Contains("Skill: autonomous-capture", step2Msg.Content);
        Assert.Contains("Pass 1 — Objective Decomposition", step2Msg.Content);
    }

    // --- Session state flows through conversation ---

    [Fact]
    public async Task Step2_MessageContainsObjectiveFromStep1()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        await agent.LoadSkillsAsync(new FakeSkillProvider());
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);

        var step2Msg = agent.Conversation.Messages
            .Where(m => m.Role == MessageRole.User)
            .Skip(1).First();
        Assert.Contains("Build personas for college athletic recruiting platform", step2Msg.Content);
    }

    [Fact]
    public async Task Step3_MessageContainsIslandsFromStep2()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        await agent.LoadSkillsAsync(new FakeSkillProvider());
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);
        await agent.ExecuteNextStepAsync(builder, client);

        var step3Msg = agent.Conversation.Messages
            .Where(m => m.Role == MessageRole.User)
            .Skip(2).First();
        Assert.Contains("ISL-001", step3Msg.Content);
        Assert.Contains("Student athlete", step3Msg.Content);
    }

    // --- System prompt includes JSON instruction ---

    [Fact]
    public async Task SystemPrompt_ContainsJsonInstruction()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        await agent.LoadSkillsAsync(new FakeSkillProvider());
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        await agent.ExecuteNextStepAsync(builder, client);

        var sysMsg = agent.Conversation.Messages.First(m => m.Role == MessageRole.System);
        Assert.Contains("valid JSON", sysMsg.Content);
    }

    // --- Full pipeline with skills ---

    [Fact]
    public async Task FullPipeline_WithSkills_MapsAllStepsToSession()
    {
        var agent = CreateAgent();
        agent.StartSession("placeholder");
        await agent.LoadSkillsAsync(new FakeSkillProvider());
        var builder = new UxStepMessageBuilder();
        var client = new FakeChatClient();

        var results = await agent.ExecuteAllStepsAsync(builder, client);

        Assert.Equal(5, results.Count);
        Assert.True(agent.IsCompleted);
        Assert.Equal("Build personas for college athletic recruiting platform", agent.Session!.Checkpoint.SessionObjective);
        Assert.Equal(3, agent.Session.Islands.Count);
        Assert.Single(agent.Session.Decisions);
        Assert.Single(agent.Session.Deliverables);
        Assert.Equal(7000, agent.Session.Checkpoint.TokensConsumption.TotalTokens);
    }

    // --- Fakes ---

    private class FakeSkillProvider : ISkillProvider
    {
        private static readonly Dictionary<string, string> SkillMds = new()
        {
            ["rehydrate-context"] = """
                ---
                name: rehydrate-context
                description: Define objective for agent and reconstruct session from MARK files.
                ---

                # Steps
                1. Read the Progress Summary MARK.
                2. Parse user input.
                3. Synthesize Session Objective with verb + deliverable + success condition + stakes.
                """,

            ["autonomous-capture"] = """
                ---
                name: autonomous-capture
                description: Generate unfiltered Island Backlog from the Session Objective.
                ---

                ### Steps
                1. Load Session Objective.
                2. Pass 1 — Objective Decomposition: sub-deliverables, decisions, risks.
                3. Pass 2 — Edge Exploration: adjacent concerns, assumptions.
                4. Tag each island: WORK, DECISION, QUESTION, RISK, DEPENDENCY, PREREQUISITE.
                5. Number sequentially: ISL-001, ISL-002, etc.
                """,

            ["strategic-organize"] = """
                ---
                name: strategic-organize
                description: Map, group, and sequence the Island Backlog into an Execution Roadmap.
                ---

                ### Steps
                1. Deduplicate.
                2. Cluster into execution groups.
                3. Sequence by dependency order.
                4. Triage questions.
                5. Emit Execution Roadmap.
                """,

            ["expert-distill"] = """
                ---
                name: expert-distill
                description: Distill each island into a concrete result or formal concern.
                ---

                ### Steps
                1. Load Distill History.
                2. Resolve blocks first.
                3. Process queue using Distill Lens.
                4. Update Results Ledger.
                """,

            ["express-relay"] = """
                ---
                name: express-relay
                description: Update MARK files and emit System Relay.
                ---

                ### Steps
                1. Compile session state.
                2. Overwrite Progress Summary MARK.
                3. Update Questions Log MARK.
                4. Emit System Relay.
                """,
        };

        public Task<Skill> LoadAsync(string skillName, CancellationToken ct = default)
        {
            var md = SkillMds[skillName];
            return Task.FromResult(Skill.FromMd(md));
        }
    }

    private class FakeChatClient : IChatClient
    {
        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            StepResult result = step.StepNumber switch
            {
                1 => new RehydrateResult(
                    Output: """{"sessionObjective":"Build personas for college athletic recruiting platform","gateSatisfied":true}""",
                    GateSatisfied: true,
                    SessionObjective: "Build personas for college athletic recruiting platform"),

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

                5 => new RelayResult(
                    Output: """{"inputTokens":2000,"outputTokens":5000,"gateSatisfied":true}""",
                    GateSatisfied: true,
                    InputTokens: 2000,
                    OutputTokens: 5000),

                _ => new StepResult($"Step {step.StepNumber}", true),
            };

            return Task.FromResult(result);
        }
    }
}
