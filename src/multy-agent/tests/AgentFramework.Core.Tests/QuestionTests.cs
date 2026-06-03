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

public class QuestionTests
{
    private const string TestDataPath = "TestData/UxAgentRole.md";

    private static async Task<UxAgent> CreateAgentAsync()
    {
        return await UxAgent.BuildAsync(UxAgentDefaults.Config(TestSteps.DefaultRole()), TestSteps.DefaultResolver());
    }

    // ==========================================================
    // Question Entity — Status Transitions
    // ==========================================================

    [Fact]
    public void NewQuestion_HasOpenStatus()
    {
        var q = new Question("Q-001", "What scope?", "capture");
        Assert.Equal(QuestionStatus.Open, q.Status);
        Assert.Null(q.Answer);
        Assert.Null(q.AnswerSource);
        Assert.Null(q.ResolvedDate);
    }

    [Fact]
    public void SetAnswer_TransitionsOpenToAnswered()
    {
        var q = new Question("Q-001", "What scope?", "capture");
        q.SetAnswer("Football only", "PjM Interview");

        Assert.Equal(QuestionStatus.Answered, q.Status);
        Assert.Equal("Football only", q.Answer);
        Assert.Equal("PjM Interview", q.AnswerSource);
    }

    [Fact]
    public void MarkReviewed_TransitionsAnsweredToReviewed()
    {
        var q = new Question("Q-001", "What scope?", "capture");
        q.SetAnswer("Football only", "PjM Interview");
        q.MarkReviewed();

        Assert.Equal(QuestionStatus.Reviewed, q.Status);
        Assert.NotNull(q.ResolvedDate);
    }

    [Fact]
    public void MarkObsolete_TransitionsFromOpen()
    {
        var q = new Question("Q-001", "What scope?", "capture");
        q.MarkObsolete();

        Assert.Equal(QuestionStatus.Obsolete, q.Status);
        Assert.NotNull(q.ResolvedDate);
    }

    [Fact]
    public void MarkObsolete_TransitionsFromAnswered()
    {
        var q = new Question("Q-001", "What scope?", "capture");
        q.SetAnswer("Football only", "PjM");
        q.MarkObsolete();

        Assert.Equal(QuestionStatus.Obsolete, q.Status);
    }

    [Fact]
    public void SetAnswer_ThrowsWhenAlreadyReviewed()
    {
        var q = new Question("Q-001", "What scope?", "capture");
        q.SetAnswer("Football only", "PjM");
        q.MarkReviewed();

        Assert.Throws<InvalidOperationException>(() => q.SetAnswer("Multi-sport", "PjM"));
    }

    [Fact]
    public void SetAnswer_ThrowsWhenAlreadyAnswered()
    {
        var q = new Question("Q-001", "What scope?", "capture");
        q.SetAnswer("Football only", "PjM");

        Assert.Throws<InvalidOperationException>(() => q.SetAnswer("Multi-sport", "PjM"));
    }

    [Fact]
    public void MarkReviewed_ThrowsWhenOpen()
    {
        var q = new Question("Q-001", "What scope?", "capture");
        Assert.Throws<InvalidOperationException>(() => q.MarkReviewed());
    }

    [Fact]
    public void MarkObsolete_ThrowsWhenAlreadyObsolete()
    {
        var q = new Question("Q-001", "What scope?", "capture");
        q.MarkObsolete();
        Assert.Throws<InvalidOperationException>(() => q.MarkObsolete());
    }

    // ==========================================================
    // AgentAggregate — Question Management
    // ==========================================================

    [Fact]
    public async Task RaiseQuestion_AddsOpenQuestion()
    {
        var agent = await CreateAgentAsync();
        agent.RaiseQuestion("UX-Q001", "What scope?", "express-relay");

        Assert.Single(agent.Questions);
        Assert.Equal("UX-Q001", agent.Questions[0].Id);
        Assert.Equal(QuestionStatus.Open, agent.Questions[0].Status);
    }

    [Fact]
    public async Task FindQuestion_ReturnsNullForMissing()
    {
        var agent = await CreateAgentAsync();
        Assert.Null(agent.FindQuestion("MISSING"));
    }

    [Fact]
    public async Task ApplyQuestionReview_TransitionsToReviewed()
    {
        var agent = await CreateAgentAsync();
        agent.RaiseQuestion("UX-Q001", "What scope?", "express-relay");
        var q = agent.FindQuestion("UX-Q001")!;
        q.SetAnswer("Football only", "PjM");

        agent.ApplyQuestionReview("UX-Q001", QuestionStatus.Reviewed);

        Assert.Equal(QuestionStatus.Reviewed, agent.Questions[0].Status);
    }

    [Fact]
    public async Task ApplyQuestionReview_TransitionsToObsolete()
    {
        var agent = await CreateAgentAsync();
        agent.RaiseQuestion("UX-Q001", "Old question", "express-relay");

        agent.ApplyQuestionReview("UX-Q001", QuestionStatus.Obsolete);

        Assert.Equal(QuestionStatus.Obsolete, agent.Questions[0].Status);
    }

    // ==========================================================
    // Aggregate — Express with Questions (via ISessionWriter)
    // ==========================================================

    [Fact]
    public async Task ApplyExpress_RaisesNewQuestions()
    {
        var agent = await CreateAgentAsync();
        var result = new ExpressResult(
            Output: "done",
            GateSatisfied: true,
            InputTokens: 100,
            OutputTokens: 200,
            Questions: [new QuestionRecord("UX-Q001", "What scope?", "open")]);

        result.ApplyTo(agent);

        Assert.Single(agent.Questions);
        Assert.Equal("UX-Q001", agent.Questions[0].Id);
        Assert.Equal(QuestionStatus.Open, agent.Questions[0].Status);
    }

    [Fact]
    public async Task ApplyExpress_ReviewsAnsweredQuestions()
    {
        var agent = await CreateAgentAsync();
        agent.RaiseQuestion("UX-Q001", "What scope?", "express-relay");
        agent.FindQuestion("UX-Q001")!.SetAnswer("Football only", "PjM");

        var result = new ExpressResult(
            Output: "done",
            GateSatisfied: true,
            InputTokens: 100,
            OutputTokens: 200,
            Questions: [new QuestionRecord("UX-Q001", "What scope?", "reviewed")]);

        result.ApplyTo(agent);

        Assert.Equal(QuestionStatus.Reviewed, agent.Questions[0].Status);
    }

    [Fact]
    public async Task ApplyExpress_MarksQuestionsObsolete()
    {
        var agent = await CreateAgentAsync();
        agent.RaiseQuestion("UX-Q001", "Old question", "express-relay");

        var result = new ExpressResult(
            Output: "done",
            GateSatisfied: true,
            InputTokens: 100,
            OutputTokens: 200,
            Questions: [new QuestionRecord("UX-Q001", "Old question", "obsolete")]);

        result.ApplyTo(agent);

        Assert.Equal(QuestionStatus.Obsolete, agent.Questions[0].Status);
    }

    [Fact]
    public async Task ApplyExpress_UpdatesTokensAndQuestions()
    {
        var agent = await CreateAgentAsync();
        ((ISessionWriter)agent).BeginIteration("objective");
        var result = new ExpressResult(
            Output: "done",
            GateSatisfied: true,
            InputTokens: 1500,
            OutputTokens: 3000,
            Questions: [
                new QuestionRecord("UX-Q001", "Question 1", "open"),
                new QuestionRecord("UX-Q002", "Question 2", "open")]);

        result.ApplyTo(agent);

        Assert.Equal(1500, agent.Session!.CurrentCheckpoint!.TokensConsumption.InputTokens);
        Assert.Equal(3000, agent.Session.CurrentCheckpoint!.TokensConsumption.OutputTokens);
        Assert.Equal(2, agent.Questions.Count);
    }

    // ==========================================================
    // AgentAggregate — Query Methods
    // ==========================================================

    [Fact]
    public async Task GetQuestions_ReturnsEmptyWhenNoSession()
    {
        var agent = await CreateAgentAsync();
        Assert.Empty(agent.GetQuestions());
    }

    [Fact]
    public async Task GetQuestions_ReturnsAllQuestions()
    {
        var agent = await CreateAgentAsync();
        agent.RaiseQuestion("Q-001", "Q1", "express");
        agent.RaiseQuestion("Q-002", "Q2", "express");

        Assert.Equal(2, agent.GetQuestions().Count);
    }

    [Fact]
    public async Task GetQuestions_FiltersByStatus()
    {
        var agent = await CreateAgentAsync();
        agent.RaiseQuestion("Q-001", "Q1", "express");
        agent.RaiseQuestion("Q-002", "Q2", "express");
        agent.FindQuestion("Q-001")!.SetAnswer("A1", "PjM");

        Assert.Single(agent.GetQuestions(QuestionStatus.Open));
        Assert.Single(agent.GetQuestions(QuestionStatus.Answered));
    }

    [Fact]
    public async Task GetOpenQuestions_ReturnsOnlyOpen()
    {
        var agent = await CreateAgentAsync();
        agent.RaiseQuestion("Q-001", "Q1", "express");
        agent.RaiseQuestion("Q-002", "Q2", "express");
        agent.FindQuestion("Q-001")!.SetAnswer("A1", "PjM");

        var open = agent.GetOpenQuestions();
        Assert.Single(open);
        Assert.Equal("Q-002", open[0].Id);
    }

    [Fact]
    public async Task GetPendingReviewQuestions_ReturnsAnswered()
    {
        var agent = await CreateAgentAsync();
        agent.RaiseQuestion("Q-001", "Q1", "express");
        agent.FindQuestion("Q-001")!.SetAnswer("A1", "PjM");

        var pending = agent.GetPendingReviewQuestions();
        Assert.Single(pending);
        Assert.Equal("Q-001", pending[0].Id);
    }

    // ==========================================================
    // AgentAggregate — SupplyAnswers
    // ==========================================================

    [Fact]
    public async Task SupplyAnswers_TransitionsOpenToAnswered()
    {
        var agent = await CreateAgentAsync();
        agent.RaiseQuestion("Q-001", "Q1", "express");
        agent.RaiseQuestion("Q-002", "Q2", "express");

        agent.SupplyAnswers([
            ("Q-001", "Answer 1", "PjM Interview"),
            ("Q-002", "Answer 2", "PjM Interview")
        ]);

        Assert.All(agent.GetQuestions(), q => Assert.Equal(QuestionStatus.Answered, q.Status));
    }

    [Fact]
    public async Task SupplyAnswers_ThrowsWhenNoSession()
    {
        var agent = await CreateAgentAsync();
        Assert.Throws<InvalidOperationException>(() =>
            agent.SupplyAnswers([("Q-001", "Answer", "PjM")]));
    }

    [Fact]
    public async Task SupplyAnswers_ThrowsForMissingQuestion()
    {
        var agent = await CreateAgentAsync();

        Assert.Throws<InvalidOperationException>(() =>
            agent.SupplyAnswers([("MISSING", "Answer", "PjM")]));
    }

    // ==========================================================
    // ExpressStep — ParseResult with Questions
    // ==========================================================

    [Fact]
    public void ExpressStep_ParseResult_ExtractsQuestions()
    {
        var step = new ExpressStep(PromptContext.Empty, 5, "instructions", new Gate("gate"));
        var json = """
            {
              "inputTokens": 1000,
              "outputTokens": 2000,
              "questions": [
                { "id": "UX-Q001", "text": "What scope?", "status": "open" },
                { "id": "UX-Q002", "text": "Geographic focus?", "status": "reviewed" }
              ],
              "gateSatisfied": true
            }
            """;
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = step.ParseResult(root, "raw output", true);

        var express = Assert.IsType<ExpressResult>(result);
        Assert.Equal(1000, express.InputTokens);
        Assert.Equal(2000, express.OutputTokens);
        Assert.Equal(2, express.Questions.Count);
        Assert.Equal("UX-Q001", express.Questions[0].Id);
        Assert.Equal("open", express.Questions[0].Status);
        Assert.Equal("UX-Q002", express.Questions[1].Id);
        Assert.Equal("reviewed", express.Questions[1].Status);
    }

    [Fact]
    public void ExpressStep_ParseResult_HandlesNoQuestions()
    {
        var step = new ExpressStep(PromptContext.Empty, 5, "instructions", new Gate("gate"));
        var json = """
            {
              "inputTokens": 500,
              "outputTokens": 1000,
              "gateSatisfied": true
            }
            """;
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var result = step.ParseResult(doc.RootElement, "raw", true);

        var express = Assert.IsType<ExpressResult>(result);
        Assert.Empty(express.Questions);
    }

    // ==========================================================
    // ExpressStep — BuildContext includes questions
    // ==========================================================

    [Fact]
    public async Task ExpressStep_BuildContext_IncludesAnsweredQuestions()
    {
        var step = new ExpressStep(PromptContext.Empty, 5, "instructions", new Gate("gate"));
        var agent = await CreateAgentAsync();
        agent.RaiseQuestion("UX-Q001", "What scope?", "express");
        agent.FindQuestion("UX-Q001")!.SetAnswer("Football only", "PjM Interview");

        var context = step.BuildContext(agent);

        Assert.Contains("UX-Q001", context);
        Assert.Contains("What scope?", context);
        Assert.Contains("Football only", context);
        Assert.Contains("PjM Interview", context);
    }

    [Fact]
    public async Task ExpressStep_BuildContext_NoQuestions_ShowsNoQuestionsLogged()
    {
        var step = new ExpressStep(PromptContext.Empty, 5, "instructions", new Gate("gate"));
        var agent = await CreateAgentAsync();

        var context = step.BuildContext(agent);

        Assert.Contains("No questions logged", context);
    }

    // ==========================================================
    // Pipeline — Express raises questions → SupplyAnswers → Kickoffludes answers
    // ==========================================================

    [Fact]
    public async Task Pipeline_QuestionsFlowBetweenSessions()
    {
        var agent = await CreateAgentAsync();
        var client = new QuestionAwareChatClient();

        // Session 1: Express raises questions
        var results = await agent.ExecuteAllStepsAsync(TestSteps.DefaultIntent, client);

        Assert.Equal(6, results.Count);
        Assert.Equal(2, agent.GetOpenQuestions().Count);

        // Between sessions: user answers questions
        agent.SupplyAnswers([
            ("UX-Q001", "Football only", "PjM Interview"),
            ("UX-Q002", "Colombia → USA", "PjM Interview")
        ]);

        Assert.Equal(2, agent.GetPendingReviewQuestions().Count);
        Assert.Empty(agent.GetOpenQuestions());
    }

    // --- Fake chat client that returns questions in Express ---

    private class QuestionAwareChatClient : IChatClient
    {
        public Task<TResult> SendHandlerAsync<TResult>(
            IReadOnlyList<ChatMessage> messages, string jsonSchema,
            Func<JsonElement, TResult> parse, CancellationToken ct = default)
        {
            string json;
            if (jsonSchema.Contains("triaged"))
                json = """{"triaged":[]}""";
            else if (jsonSchema.Contains("groupDistillations"))
                json = """{"groupDistillations":[{"groupId":"GRP-001","decisions":[{"id":"DEC-001","description":"Merge pain into athlete","impact":"Reduces count"}],"deliverables":[{"deliverableId":"DEL-001","path":"outputs/personas/01-athlete.md","purpose":"Athlete card","status":"Complete"}],"questions":[]}],"distilledIslands":[{"islandId":"ISL-001","newStatus":"Distilled"},{"islandId":"ISL-002","newStatus":"Distilled"}],"gateSatisfied":true}""";
            else if (jsonSchema.Contains("readiness"))
                json = """{"groups":[{"id":"GRP-001","name":"Recruiting Group","islandIds":["ISL-001","ISL-002"],"whyTogether":"Both describe recruiting","readiness":"Ready","readinessNotes":null}]}""";
            else if (jsonSchema.Contains("islandIds"))
                json = """{"groups":[{"id":"GRP-001","name":"Recruiting Group","islandIds":["ISL-001","ISL-002"],"whyTogether":"Both describe recruiting"}],"ungroupedIslandIds":["ISL-003"]}""";
            else if (jsonSchema.Contains("islands"))
                json = """{"islands":[{"id":"ISL-001","type":"UserType","description":"Student athlete","source":"six-hats:yellow","relatesToIslandId":null},{"id":"ISL-002","type":"Stakeholder","description":"College coach","source":"six-hats:white","relatesToIslandId":null},{"id":"ISL-003","type":"PainPoint","description":"No visibility","source":"six-hats:black","relatesToIslandId":"ISL-001"}]}""";
            else if (jsonSchema.Contains("\"html\""))
                json = """{"html":"<html><body>Test Document</body></html>"}""";
            else if (jsonSchema.Contains("inputTokens"))
                json = """{"questions":[{"id":"UX-Q001","text":"Is scope football-only?","status":"open"},{"id":"UX-Q002","text":"Geographic scope?","status":"open"}],"inputTokens":2000,"outputTokens":5000,"gateSatisfied":true}""";
            else
                json = """{"sessionObjective":"Build personas","narrativeBridge":"Initial session.","isInitialSession":true,"stalenessWarning":null,"gateSatisfied":true}""";
            return Task.FromResult(parse(JsonDocument.Parse(json).RootElement));
        }

        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            StepResult result = step.StepNumber switch
            {
                1 => new KickoffResult("Objective defined", true, "Build personas"),
                2 => new CaptureResult("Captured", true, [
                    new CapturedIsland("ISL-001", IslandType.UserType, "Student athlete", "product"),
                    new CapturedIsland("ISL-002", IslandType.Stakeholder, "Coach", "product"),
                    new CapturedIsland("ISL-003", IslandType.PainPoint, "No visibility", "interview", "ISL-001")
                ]),
                3 => new OrganizeResult("Organized", true,
                    [new IslandOrganization("ISL-001", IslandStatus.Organized), new IslandOrganization("ISL-002", IslandStatus.Organized), new IslandOrganization("ISL-003", IslandStatus.Discarded)],
                    [new DecisionRecord("DEC-001", "Merge pain into athlete", "Reduces count")]),
                4 => new DistillResult("Distilled", true,
                    [new IslandDistillation("ISL-001", IslandStatus.Distilled), new IslandDistillation("ISL-002", IslandStatus.Distilled)],
                    [new DeliverableRecord("DEL-001", "outputs/personas/01-athlete.md", DeliverableStatus.Complete)]),
                5 => new ExpressResult("Express emitted", true, 2000, 5000, [
                    new QuestionRecord("UX-Q001", "Is scope football-only?", "open"),
                    new QuestionRecord("UX-Q002", "Geographic scope?", "open")
                ]),
                _ => new StepResult($"Step {step.StepNumber}", true),
            };
            return Task.FromResult(result);
        }
    }
}
