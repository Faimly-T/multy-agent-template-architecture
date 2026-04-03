using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Events;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Domain.UxAgent;

namespace AgentFramework.Core.Tests;

public class StepPipelineTests
{
    private const string TestDataPath = "TestData/UxPersonaRole.md";

    private static UxPersona CreateAgent()
    {
        var markdown = File.ReadAllText(TestDataPath);
        return UxPersonaFactory.Create(markdown);
    }

    [Fact]
    public void UxPersona_Has5Steps()
    {
        var agent = CreateAgent();
        Assert.Equal(5, agent.Steps.Count);
    }

    [Fact]
    public void Steps_AreNumberedSequentially()
    {
        var agent = CreateAgent();
        for (int i = 0; i < agent.Steps.Count; i++)
        {
            Assert.Equal(i + 1, agent.Steps[i].StepNumber);
        }
    }

    [Fact]
    public void Step1_UsesRehydrateContext()
    {
        var agent = CreateAgent();
        Assert.Equal("rehydrate-context", agent.Steps[0].SkillName);
        Assert.Equal("Objective confirmed", agent.Steps[0].Gate.Description);
    }

    [Fact]
    public void Step2_UsesAutonomousCapture()
    {
        var agent = CreateAgent();
        Assert.Equal("autonomous-capture", agent.Steps[1].SkillName);
        Assert.Equal("≥3 user-type islands", agent.Steps[1].Gate.Description);
    }

    [Fact]
    public void NewAgent_StartsAtStep0_NotCompleted()
    {
        var agent = CreateAgent();
        Assert.Equal(0, agent.CurrentStepIndex);
        Assert.False(agent.IsCompleted);
    }

    [Fact]
    public async Task ExecuteNextStep_RaisesStepStartedAndCompleted_WhenGatePasses()
    {
        var agent = CreateAgent();
        var executor = new FakeStepExecutor(gateSatisfied: true);

        var result = await agent.ExecuteNextStepAsync(executor);

        Assert.True(result.GateSatisfied);
        Assert.Equal(1, agent.CurrentStepIndex);
        Assert.Equal(2, agent.DomainEvents.Count);
        Assert.IsType<StepStarted>(agent.DomainEvents[0]);
        Assert.IsType<StepCompleted>(agent.DomainEvents[1]);
    }

    [Fact]
    public async Task ExecuteNextStep_RaisesStepGateFailed_WhenGateFails()
    {
        var agent = CreateAgent();
        var executor = new FakeStepExecutor(gateSatisfied: false);

        var result = await agent.ExecuteNextStepAsync(executor);

        Assert.False(result.GateSatisfied);
        Assert.Equal(0, agent.CurrentStepIndex); // did not advance
        Assert.Equal(2, agent.DomainEvents.Count);
        Assert.IsType<StepStarted>(agent.DomainEvents[0]);
        Assert.IsType<StepGateFailed>(agent.DomainEvents[1]);
    }

    [Fact]
    public async Task ExecuteAllSteps_RunsAllSteps_WhenAllGatesPass()
    {
        var agent = CreateAgent();
        var executor = new FakeStepExecutor(gateSatisfied: true);

        var results = await agent.ExecuteAllStepsAsync(executor);

        Assert.Equal(5, results.Count);
        Assert.True(agent.IsCompleted);
        Assert.All(results, r => Assert.True(r.GateSatisfied));
    }

    [Fact]
    public async Task ExecuteAllSteps_StopsAtFailedGate()
    {
        var agent = CreateAgent();
        var executor = new FakeStepExecutor(failAtStep: 3);

        var results = await agent.ExecuteAllStepsAsync(executor);

        Assert.Equal(3, results.Count);
        Assert.False(agent.IsCompleted);
        Assert.Equal(2, agent.CurrentStepIndex); // stopped at step 3 (index 2)
    }

    [Fact]
    public async Task ExecuteNextStep_ThrowsWhenAllCompleted()
    {
        var agent = CreateAgent();
        var executor = new FakeStepExecutor(gateSatisfied: true);
        await agent.ExecuteAllStepsAsync(executor);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => agent.ExecuteNextStepAsync(executor));
    }

    [Fact]
    public async Task StepStarted_CarriesCorrectSkillName()
    {
        var agent = CreateAgent();
        var executor = new FakeStepExecutor(gateSatisfied: true);

        await agent.ExecuteNextStepAsync(executor);

        var started = (StepStarted)agent.DomainEvents[0];
        Assert.Equal("rehydrate-context", started.SkillName);
        Assert.Equal(1, started.StepNumber);
    }

    [Fact]
    public async Task StepCompleted_CarriesResult()
    {
        var agent = CreateAgent();
        var executor = new FakeStepExecutor(gateSatisfied: true);

        await agent.ExecuteNextStepAsync(executor);

        var completed = (StepCompleted)agent.DomainEvents[1];
        Assert.Equal("Fake output for step 1", completed.Result.Output);
    }

    [Fact]
    public async Task ClearDomainEvents_EmptiesTheList()
    {
        var agent = CreateAgent();
        var executor = new FakeStepExecutor(gateSatisfied: true);
        await agent.ExecuteNextStepAsync(executor);

        agent.ClearDomainEvents();

        Assert.Empty(agent.DomainEvents);
    }

    private class FakeStepExecutor : IStepExecutor
    {
        private readonly bool _gateSatisfied;
        private readonly int? _failAtStep;

        public FakeStepExecutor(bool gateSatisfied = true, int? failAtStep = null)
        {
            _gateSatisfied = gateSatisfied;
            _failAtStep = failAtStep;
        }

        public Task<StepResult> ExecuteAsync(AgentStep step, Role role, CancellationToken ct = default)
        {
            var passed = _failAtStep.HasValue
                ? step.StepNumber != _failAtStep.Value
                : _gateSatisfied;

            return Task.FromResult(new StepResult(
                Output: $"Fake output for step {step.StepNumber}",
                GateSatisfied: passed));
        }
    }
}
