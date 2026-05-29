using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.Core.Agent.Steps.CODESteps;
using AgentFramework.Domain.UxAgent;
using AgentFramework.Infrastructure;
using AgentFramework.Infrastructure.Anthropic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.Core.Tests;

public class UxPersonaTests
{
    private const string TestRoleDataPath = "TestData/UxPersonaRole.md";

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build();

    private static ServiceProvider BuildServiceProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddAgentInfrastructure();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Factory_CreatesUxPersona_WithRoleLoadedFromMd()
    {
        var markdown = File.ReadAllText(TestRoleDataPath);
        var role     = RoleParser.ParseFromMarkdown(markdown);
        var config   = TestSteps.DefaultUxConfig(role);
        var agent    = await UxPersona.BuildAsync(config, TestSteps.DefaultPipelineCode());

        Assert.Equal("ux-persona-test", agent.Id);
        Assert.Equal(5, agent.Steps.Count);
        Assert.NotNull(agent.Session);
    }

    [Fact]
    public async Task Factory_CreatesUxPersona_WithStepsLoadedFromMd()
    {
        var markdown = File.ReadAllText(TestRoleDataPath);
        var role     = RoleParser.ParseFromMarkdown(markdown);
        var config   = TestSteps.DefaultUxConfig(role);
        var agent    = await UxPersona.BuildAsync(config, TestSteps.DefaultPipelineCode());

        Assert.Equal("ux-persona-test", agent.Id);
        Assert.Equal("Kickoff-context", agent.Steps[0].SkillName);
        Assert.Equal("autonomous-capture", agent.Steps[1].SkillName);
        Assert.Equal("strategic-organize", agent.Steps[2].SkillName);
        Assert.Equal("expert-distill", agent.Steps[3].SkillName);
        Assert.Equal("express-relay", agent.Steps[4].SkillName);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExecuteFirstStep_WithAnthropicLlm_ReturnsKickoffResult()
    {
        var config = BuildConfiguration();
        var apiKey = config.GetSection(AnthropicOptions.SectionName)["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return;

        await using var sp = BuildServiceProvider(config);
        var chatClient = sp.GetRequiredService<IChatClient>();

        var markdown    = File.ReadAllText(TestRoleDataPath);
        var role        = RoleParser.ParseFromMarkdown(markdown);
        var agentConfig = TestSteps.DefaultUxConfig(role);
        var agent       = await UxPersona.BuildAsync(agentConfig, TestSteps.DefaultPipelineCode());

        agent.SetUserIntent("Analyze a college athletic recruiting platform that connects high-school athletes with university scouts.");

        var result = await agent.ExecuteNextStepAsync(chatClient);

        Assert.NotNull(result);
        Assert.IsType<KickoffResult>(result);

        var kickoff = (KickoffResult)result;
        Assert.True(kickoff.GateSatisfied, $"Gate not satisfied. Output: {kickoff.Output}");
        Assert.NotEmpty(kickoff.SessionObjective);

        Assert.NotNull(agent.Session);
        Assert.Contains(agent.Session.CurrentCheckpoint!.SessionObjective, kickoff.SessionObjective);
        Assert.True(agent.ConversationMessages.Count >= 3);
        Assert.Equal(1, agent.Pipeline.CurrentStepIndex);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UxPersona_FullProcess_RunAsync_CompletesAllFiveCodeSteps()
    {
        var config = BuildConfiguration();
        var apiKey = config.GetSection(AnthropicOptions.SectionName)["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return;

        await using var sp = BuildServiceProvider(config);
        var chatClient = sp.GetRequiredService<IChatClient>();

        var markdown    = File.ReadAllText(TestRoleDataPath);
        var role        = RoleParser.ParseFromMarkdown(markdown);
        var agentConfig = TestSteps.DefaultUxConfig(role);
        var agent       = await UxPersona.BuildAsync(agentConfig, TestSteps.DefaultPipelineCode());

        var deliverableWriter = new TestDeliverableWriter();

        var runResult = await agent.RunAsync(
            "Analyze a college athletic recruiting platform that connects high-school athletes with university scouts.",
            chatClient,
            deliverableWriter);

        Assert.True(runResult.Completed);
        Assert.Equal(5, runResult.StepResults.Count);
        Assert.NotNull(runResult.Session);

        Assert.IsType<KickoffResult>(runResult.StepResults[0]);
        Assert.IsType<CaptureResult>(runResult.StepResults[1]);
        Assert.IsType<OrganizeResult>(runResult.StepResults[2]);
        Assert.IsType<DistillResult>(runResult.StepResults[3]);
        Assert.IsType<ExpressResult>(runResult.StepResults[4]);

        Assert.All(runResult.StepResults, result => Assert.True(result.GateSatisfied));

        Assert.NotEmpty(runResult.Deliverables);
        Assert.True(runResult.Session.CurrentCheckpoint!.SessionObjective.Contains("recruiting platform", StringComparison.OrdinalIgnoreCase));
        Assert.True(runResult.Session.CurrentCheckpoint!.TokensConsumption.InputTokens >= 0);
        Assert.True(runResult.Session.CurrentCheckpoint!.TokensConsumption.OutputTokens >= 0);
        Assert.True(deliverableWriter.WasWritten);
    }

    private class TestDeliverableWriter : IDeliverableWriter
    {
        public bool WasWritten { get; private set; }

        public Task WriteAsync(IAgentRunContext context, IReadOnlyList<StepResult> results, CancellationToken ct = default)
        {
            WasWritten = true;
            return Task.CompletedTask;
        }
    }
}
