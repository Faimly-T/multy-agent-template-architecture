using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Prompts;
using AgentFramework.Core.Agent.Session;
using AgentFramework.Core.Agent.Steps;
using AgentFramework.CodePipeline;
using AgentFramework.Domain.UxAgent;
using AgentFramework.Domain.UxAgent.Express;
using AgentFramework.Infrastructure;
using AgentFramework.Infrastructure.Anthropic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.Core.Tests;

public class UxAgentTests
{
    private const string TestRoleDataPath = "TestData/UxAgentRole.md";

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
    public async Task Factory_CreatesUxAgent_WithRoleLoadedFromMd()
    {
        var agent = await UxAgent.BuildAsync(UxAgentDefaults.Config(TestSteps.DefaultRole()), TestSteps.DefaultResolver());

        Assert.Equal("ux-agent", agent.Id);
        Assert.Equal(6, agent.Steps.Count);
        Assert.NotNull(agent.Session);
    }

    [Fact]
    public async Task Factory_CreatesUxAgent_WithCorrectSkillsLoadedPerStep()
    {
        var agent = await UxAgent.BuildAsync(UxAgentDefaults.Config(TestSteps.DefaultRole()), TestSteps.DefaultResolver());

        // Verify actual resolved skills — not hardcoded labels
        Assert.Contains("Kickoff-context",        agent.Steps[0].SkillNames);
        Assert.Contains("six-thinking-hats",      agent.Steps[1].SkillNames);
        Assert.Contains("capture-strict-islands", agent.Steps[1].SkillNames);
        Assert.Contains("strategic-organize",     agent.Steps[2].SkillNames);
        Assert.Contains("expert-distill",         agent.Steps[3].SkillNames);
        Assert.Contains("express-relay",          agent.Steps[4].SkillNames);
        Assert.Empty(agent.Steps[5].SkillNames);  // ClosedStep: no skills needed
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
        var agent       = await UxAgent.BuildAsync(agentConfig, TestSteps.DefaultResolver());

        var result = await agent.ExecuteNextStepAsync(
            new AgentRequest("Analyze a college athletic recruiting platform that connects high-school athletes with university scouts."),
            chatClient);

        Assert.NotNull(result);
        Assert.IsType<KickoffResult>(result);

        var kickoff = (KickoffResult)result;
        Assert.True(kickoff.GateSatisfied, $"Gate not satisfied. Output: {kickoff.Output}");
        Assert.NotEmpty(kickoff.SessionObjective);

        Assert.NotNull(agent.Session);
        Assert.Contains(agent.Session.CurrentCheckpoint!.SessionObjective, kickoff.SessionObjective);
        Assert.NotEmpty(agent.GetStepJournal("KickoffStep"));
        Assert.Equal(1, agent.Pipeline.CurrentStepIndex);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UxAgent_FullProcess_RunAsync_CompletesAllSixCodeSteps()
    {
        var config = BuildConfiguration();
        var apiKey = config.GetSection(AnthropicOptions.SectionName)["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return;

        await using var sp = BuildServiceProvider(config);
        var chatClient = sp.GetRequiredService<IChatClient>();

        var markdown    = File.ReadAllText(TestRoleDataPath);
        var role        = RoleParser.ParseFromMarkdown(markdown);
        var agentConfig = TestSteps.DefaultUxConfig(role);
        var agent       = await UxAgent.BuildAsync(agentConfig, TestSteps.DefaultResolver());

        var runResult = await agent.RunAsync(
            "Analyze a college athletic recruiting platform that connects high-school athletes with university scouts.",
            chatClient,
            NullDeliverableWriter.Instance);

        Assert.True(runResult.Completed);
        Assert.Equal(6, runResult.StepResults.Count);
        Assert.NotNull(runResult.Session);

        Assert.IsType<KickoffResult>(runResult.StepResults[0]);
        Assert.IsType<CaptureResult>(runResult.StepResults[1]);
        Assert.IsType<OrganizeResult>(runResult.StepResults[2]);
        Assert.IsType<DistillResult>(runResult.StepResults[3]);
        Assert.IsType<UxExpressResult>(runResult.StepResults[4]);
        Assert.IsType<ClosedResult>(runResult.StepResults[5]);

        Assert.All(runResult.StepResults, result => Assert.True(result.GateSatisfied));

        Assert.NotEmpty(runResult.Deliverables);
        Assert.True(runResult.Session.CurrentCheckpoint!.SessionObjective.Contains("recruiting platform", StringComparison.OrdinalIgnoreCase));
        Assert.True(runResult.Session.CurrentCheckpoint!.TokensConsumption.InputTokens >= 0);
        Assert.True(runResult.Session.CurrentCheckpoint!.TokensConsumption.OutputTokens >= 0);
    }
}
