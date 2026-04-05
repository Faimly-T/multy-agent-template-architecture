using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Agent.Steps.CODESteps;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class QuestionTests
{
    private const string TestDataPath = "TestData/UxPersonaRole.md";

    private static UxPersona CreateAgent()
    {
        var markdown = File.ReadAllText(TestDataPath);
        return new UxPersona(Role.FromMd(markdown), TestSteps.DefaultSteps());
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
    // AgentSession — Question Management
    // ==========================================================

    [Fact]
    public void RaiseQuestion_AddsOpenQuestion()
    {
        var session = new AgentSession("objective");
        session.RaiseQuestion("UX-Q001", "What scope?", "express-relay");

        Assert.Single(session.Questions);
        Assert.Equal("UX-Q001", session.Questions[0].Id);
        Assert.Equal(QuestionStatus.Open, session.Questions[0].Status);
    }

    [Fact]
    public void FindQuestion_ReturnsNullForMissing()
    {
        var session = new AgentSession("objective");
        Assert.Null(session.FindQuestion("MISSING"));
    }

    [Fact]
    public void ApplyQuestionReview_TransitionsToReviewed()
    {
        var session = new AgentSession("objective");
        session.RaiseQuestion("UX-Q001", "What scope?", "express-relay");
        var q = session.FindQuestion("UX-Q001")!;
        q.SetAnswer("Football only", "PjM");

        session.ApplyQuestionReview("UX-Q001", QuestionStatus.Reviewed);

        Assert.Equal(QuestionStatus.Reviewed, session.Questions[0].Status);
    }

    [Fact]
    public void ApplyQuestionReview_TransitionsToObsolete()
    {
        var session = new AgentSession("objective");
        session.RaiseQuestion("UX-Q001", "Old question", "express-relay");

        session.ApplyQuestionReview("UX-Q001", QuestionStatus.Obsolete);

        Assert.Equal(QuestionStatus.Obsolete, session.Questions[0].Status);
    }

    // ==========================================================
    // AgentSession.Apply — Express with Questions
    // ==========================================================

    [Fact]
    public void ApplyExpress_RaisesNewQuestions()
    {
        var session = new AgentSession("objective");
        var result = new ExpressResult(
            Output: "done",
            GateSatisfied: true,
            InputTokens: 100,
            OutputTokens: 200,
            Questions: [new QuestionRecord("UX-Q001", "What scope?", "open")]);

        session.Apply(result);

        Assert.Single(session.Questions);
        Assert.Equal("UX-Q001", session.Questions[0].Id);
        Assert.Equal(QuestionStatus.Open, session.Questions[0].Status);
    }

    [Fact]
    public void ApplyExpress_ReviewsAnsweredQuestions()
    {
        var session = new AgentSession("objective");
        session.RaiseQuestion("UX-Q001", "What scope?", "express-relay");
        session.FindQuestion("UX-Q001")!.SetAnswer("Football only", "PjM");

        var result = new ExpressResult(
            Output: "done",
            GateSatisfied: true,
            InputTokens: 100,
            OutputTokens: 200,
            Questions: [new QuestionRecord("UX-Q001", "What scope?", "reviewed")]);

        session.Apply(result);

        Assert.Equal(QuestionStatus.Reviewed, session.Questions[0].Status);
    }

    [Fact]
    public void ApplyExpress_MarksQuestionsObsolete()
    {
        var session = new AgentSession("objective");
        session.RaiseQuestion("UX-Q001", "Old question", "express-relay");

        var result = new ExpressResult(
            Output: "done",
            GateSatisfied: true,
            InputTokens: 100,
            OutputTokens: 200,
            Questions: [new QuestionRecord("UX-Q001", "Old question", "obsolete")]);

        session.Apply(result);

        Assert.Equal(QuestionStatus.Obsolete, session.Questions[0].Status);
    }

    [Fact]
    public void ApplyExpress_UpdatesTokensAndQuestions()
    {
        var session = new AgentSession("objective");
        var result = new ExpressResult(
            Output: "done",
            GateSatisfied: true,
            InputTokens: 1500,
            OutputTokens: 3000,
            Questions: [
                new QuestionRecord("UX-Q001", "Question 1", "open"),
                new QuestionRecord("UX-Q002", "Question 2", "open")]);

        session.Apply(result);

        Assert.Equal(1500, session.Checkpoint.TokensConsumption.InputTokens);
        Assert.Equal(3000, session.Checkpoint.TokensConsumption.OutputTokens);
        Assert.Equal(2, session.Questions.Count);
    }

    // ==========================================================
    // AgentAggregate — Query Methods
    // ==========================================================

    [Fact]
    public void GetQuestions_ReturnsEmptyWhenNoSession()
    {
        var agent = CreateAgent();
        Assert.Empty(agent.GetQuestions());
    }

    [Fact]
    public void GetQuestions_ReturnsAllQuestions()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");
        session.RaiseQuestion("Q-001", "Q1", "express");
        session.RaiseQuestion("Q-002", "Q2", "express");

        Assert.Equal(2, agent.GetQuestions().Count);
    }

    [Fact]
    public void GetQuestions_FiltersByStatus()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");
        session.RaiseQuestion("Q-001", "Q1", "express");
        session.RaiseQuestion("Q-002", "Q2", "express");
        session.FindQuestion("Q-001")!.SetAnswer("A1", "PjM");

        Assert.Single(agent.GetQuestions(QuestionStatus.Open));
        Assert.Single(agent.GetQuestions(QuestionStatus.Answered));
    }

    [Fact]
    public void GetOpenQuestions_ReturnsOnlyOpen()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");
        session.RaiseQuestion("Q-001", "Q1", "express");
        session.RaiseQuestion("Q-002", "Q2", "express");
        session.FindQuestion("Q-001")!.SetAnswer("A1", "PjM");

        var open = agent.GetOpenQuestions();
        Assert.Single(open);
        Assert.Equal("Q-002", open[0].Id);
    }

    [Fact]
    public void GetPendingReviewQuestions_ReturnsAnswered()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");
        session.RaiseQuestion("Q-001", "Q1", "express");
        session.FindQuestion("Q-001")!.SetAnswer("A1", "PjM");

        var pending = agent.GetPendingReviewQuestions();
        Assert.Single(pending);
        Assert.Equal("Q-001", pending[0].Id);
    }

    // ==========================================================
    // AgentAggregate — SupplyAnswers
    // ==========================================================

    [Fact]
    public void SupplyAnswers_TransitionsOpenToAnswered()
    {
        var agent = CreateAgent();
        var session = agent.StartSession("objective");
        session.RaiseQuestion("Q-001", "Q1", "express");
        session.RaiseQuestion("Q-002", "Q2", "express");

        agent.SupplyAnswers([
            ("Q-001", "Answer 1", "PjM Interview"),
            ("Q-002", "Answer 2", "PjM Interview")
        ]);

        Assert.All(agent.GetQuestions(), q => Assert.Equal(QuestionStatus.Answered, q.Status));
    }

    [Fact]
    public void SupplyAnswers_ThrowsWhenNoSession()
    {
        var agent = CreateAgent();
        Assert.Throws<InvalidOperationException>(() =>
            agent.SupplyAnswers([("Q-001", "Answer", "PjM")]));
    }

    [Fact]
    public void SupplyAnswers_ThrowsForMissingQuestion()
    {
        var agent = CreateAgent();
        agent.StartSession("objective");

        Assert.Throws<InvalidOperationException>(() =>
            agent.SupplyAnswers([("MISSING", "Answer", "PjM")]));
    }

    // ==========================================================
    // ExpressStep — ParseResult with Questions
    // ==========================================================

    [Fact]
    public void ExpressStep_ParseResult_ExtractsQuestions()
    {
        var step = new ExpressStep(5, "Express", "instructions", new Gate("gate"));
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
        var step = new ExpressStep(5, "Express", "instructions", new Gate("gate"));
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
    public void ExpressStep_BuildContext_IncludesAnsweredQuestions()
    {
        var step = new ExpressStep(5, "Express", "instructions", new Gate("gate"));
        var session = new AgentSession("objective");
        session.RaiseQuestion("UX-Q001", "What scope?", "express");
        session.FindQuestion("UX-Q001")!.SetAnswer("Football only", "PjM Interview");

        var context = step.BuildContext(session);

        Assert.Contains("UX-Q001", context);
        Assert.Contains("What scope?", context);
        Assert.Contains("Football only", context);
        Assert.Contains("PjM Interview", context);
    }

    [Fact]
    public void ExpressStep_BuildContext_NoQuestions_ShowsNoQuestionsLogged()
    {
        var step = new ExpressStep(5, "Express", "instructions", new Gate("gate"));
        var session = new AgentSession("objective");

        var context = step.BuildContext(session);

        Assert.Contains("No questions logged", context);
    }

    // ==========================================================
    // RehydrateStep — BuildContext includes answered questions
    // ==========================================================

    [Fact]
    public void RehydrateStep_BuildContext_IncludesAnsweredQuestions()
    {
        var step = new RehydrateStep(1, "Rehydrate", "instructions", new Gate("gate"));
        var session = new AgentSession("Build personas");
        session.RaiseQuestion("UX-Q001", "What scope?", "express");
        session.FindQuestion("UX-Q001")!.SetAnswer("Football only", "PjM Interview");

        var context = step.BuildContext(session);

        Assert.Contains("Answered questions from prior session", context);
        Assert.Contains("UX-Q001", context);
        Assert.Contains("Football only", context);
        Assert.Contains("Express step", context);
    }

    [Fact]
    public void RehydrateStep_BuildContext_NoAnsweredQuestions_OmitsBlock()
    {
        var step = new RehydrateStep(1, "Rehydrate", "instructions", new Gate("gate"));
        var session = new AgentSession("Build personas");
        // Open question, not answered
        session.RaiseQuestion("UX-Q001", "What scope?", "express");

        var context = step.BuildContext(session);

        Assert.DoesNotContain("Answered questions", context);
        Assert.Contains("Build personas", context);
    }

    // ==========================================================
    // Pipeline — Express raises questions → SupplyAnswers → Rehydrate includes answers
    // ==========================================================

    [Fact]
    public async Task Pipeline_QuestionsFlowBetweenSessions()
    {
        var agent = CreateAgent();
        var builder = new UxStepMessageBuilder();
        var client = new QuestionAwareChatClient();

        // Session 1: Express raises questions
        agent.StartSession("Build personas");
        var results = await agent.ExecuteAllStepsAsync(builder, client);

        Assert.Equal(5, results.Count);
        Assert.Equal(2, agent.GetOpenQuestions().Count);

        // Between sessions: user answers questions
        agent.SupplyAnswers([
            ("UX-Q001", "Football only", "PjM Interview"),
            ("UX-Q002", "Colombia → USA", "PjM Interview")
        ]);

        Assert.Equal(2, agent.GetPendingReviewQuestions().Count);
        Assert.Empty(agent.GetOpenQuestions());
    }

    [Fact]
    public async Task Pipeline_ExpressStep_RaisesQuestionsUpdatedEvent()
    {
        var agent = CreateAgent();
        var builder = new UxStepMessageBuilder();
        var client = new QuestionAwareChatClient();

        agent.StartSession("Build personas");
        await agent.ExecuteAllStepsAsync(builder, client);

        var questionsEvent = agent.DomainEvents.OfType<QuestionsUpdated>().SingleOrDefault();
        Assert.NotNull(questionsEvent);
        Assert.Equal(2, questionsEvent.NewQuestions.Count);
        Assert.Equal(0, questionsEvent.ReviewedCount);
    }

    // --- Fake chat client that returns questions in Express ---

    private class QuestionAwareChatClient : IChatClient
    {
        public Task<StepResult> SendAsync(IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
        {
            StepResult result = step.StepNumber switch
            {
                1 => new RehydrateResult("Objective defined", true, "Build personas"),
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
