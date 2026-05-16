using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Ports;
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
    private const string TestRehydrateDataPath = "TestData/Skills/rehydrate-context.md";

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
    public void Factory_CreatesUxPersona_WithRoleLoadedFromMd()
    {
        var markdown = File.ReadAllText(TestRoleDataPath);
        var agent = new UxPersona(RoleParser.ParseFromMarkdown(markdown), TestSteps.DefaultSteps(), TestSteps.DefaultSkills());

        Assert.Equal("ux-persona-architect", agent.Id);
        Assert.Equal("ux-persona-architect", agent.Role.Name);
        Assert.Equal("Senior UX Researcher & Persona Architect", agent.Role.Description);
        Assert.NotEmpty(agent.Role.Identity.Role);
        Assert.NotEmpty(agent.Role.Mandate);
        Assert.Equal(7, agent.Role.FactsAndDirectives.Count);
    }

    [Fact]
    public void Factory_CreatesUxPersona_WithStepsLoadedFromMd()
    {
        // Arrange - load role and steps from markdown, build skills, and inject into persona
        var markdownRole = File.ReadAllText(TestRoleDataPath);

        //Act - build steps and pass skills to persona (StepPipeline owns attachment)
        var agent = new UxPersona(RoleParser.ParseFromMarkdown(markdownRole), TestSteps.DefaultSteps(), TestSteps.DefaultSkills());

        //Assert - verify persona has role and steps with skills attached
        Assert.Equal("ux-persona-architect", agent.Id);
        Assert.Equal("rehydrate-context", agent.Steps[0].SkillName);
        Assert.Equal("autonomous-capture", agent.Steps[1].SkillName);
        Assert.Equal("strategic-organize", agent.Steps[2].SkillName);
        Assert.Equal("expert-distill", agent.Steps[3].SkillName);
        Assert.Equal("express-relay", agent.Steps[4].SkillName);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExecuteFirstStep_WithAnthropicLlm_ReturnsRehydrateResult()
    {
        // Arrange — builds DI container from appsettings.json + appsettings.local.json
        var config = BuildConfiguration();
        var apiKey = config.GetSection(AnthropicOptions.SectionName)["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // Silently pass when no key is configured (CI, local dev without key)
            return;
        }

        await using var sp = BuildServiceProvider(config);
        var chatClient = sp.GetRequiredService<IChatClient>();

        var markdown = File.ReadAllText(TestRoleDataPath);
        var agent = new UxPersona(RoleParser.ParseFromMarkdown(markdown), TestSteps.DefaultSteps(), TestSteps.DefaultSkills());

        agent.OpenSession("test-proj", new SessionMarkFilePaths("UX", "outputs/contextAgent"),
            "Analyze a college athletic recruiting platform that connects high-school athletes with university scouts.");

        var messageBuilder = new UxStepMessageBuilder();

        // Act — execute only step 1 (rehydrate-context)
        var result = await agent.ExecuteNextStepAsync(messageBuilder, chatClient);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<RehydrateResult>(result);

        var rehydrate = (RehydrateResult)result;
        Assert.True(rehydrate.GateSatisfied, $"Gate not satisfied. Output: {rehydrate.Output}");
        Assert.NotEmpty(rehydrate.SessionObjective);

        // Session should be updated
        Assert.NotNull(agent.Session);
        Assert.Contains(agent.Session.CurrentCheckpoint!.SessionObjective, rehydrate.SessionObjective);

        // Conversation should have system + user + assistant messages
        Assert.True(agent.ConversationMessages.Count >= 3);

        // Step index should have advanced
        Assert.Equal(1, agent.Pipeline.CurrentStepIndex);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UxPersona_FullProcess_RunAsync_CompletesAllFiveCodeSteps()
    {
        // Arrange — builds DI container from appsettings.json + appsettings.local.json
        var config = BuildConfiguration();
        var apiKey = config.GetSection(AnthropicOptions.SectionName)["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // Silently pass when no key is configured (CI, local dev without key)
            return;
        }

        await using var sp = BuildServiceProvider(config);
        var chatClient = sp.GetRequiredService<IChatClient>();

        var markdown = File.ReadAllText(TestRoleDataPath);
        var agent = new UxPersona(RoleParser.ParseFromMarkdown(markdown), TestSteps.DefaultSteps(), TestSteps.DefaultSkills());

        // Create test implementations
        var pipelineFactory = new TestPipelineFactory();
        var deliverableWriter = new TestDeliverableWriter();

        agent.OpenSession("test-proj", new SessionMarkFilePaths("UX", "outputs/contextAgent"));

        // Act — execute full process via RunAsync
        var runResult = await agent.RunAsync(
            "Analyze a college athletic recruiting platform that connects high-school athletes with university scouts.",
            chatClient,
            new TestSkillProvider(),
            deliverableWriter,
            pipelineFactory,
            new UxStepMessageBuilder());

        // Assert
        Assert.True(runResult.Completed);
        Assert.Equal(5, runResult.StepResults.Count);
        Assert.NotNull(runResult.Session);

        Assert.IsType<RehydrateResult>(runResult.StepResults[0]);
        Assert.IsType<CaptureResult>(runResult.StepResults[1]);
        Assert.IsType<OrganizeResult>(runResult.StepResults[2]);
        Assert.IsType<DistillResult>(runResult.StepResults[3]);
        Assert.IsType<ExpressResult>(runResult.StepResults[4]);

        Assert.All(runResult.StepResults, result => Assert.True(result.GateSatisfied));

        Assert.NotEmpty(runResult.Deliverables);
        Assert.True(runResult.Session.CurrentCheckpoint!.SessionObjective.Contains("recruiting platform", StringComparison.OrdinalIgnoreCase));
        Assert.True(runResult.Session.CurrentCheckpoint!.TokensConsumption.InputTokens >= 0);
        Assert.True(runResult.Session.CurrentCheckpoint!.TokensConsumption.OutputTokens >= 0);

        // Verify deliverables were written
        Assert.True(deliverableWriter.WasWritten);
    }

    // Test implementations
    private class TestPipelineFactory : IPipelineFactory
    {
        public Task<StepPipeline> CreatePipelineAsync(Role role, ISkillProvider skillProvider, CancellationToken ct = default)
        {
            return Task.FromResult(TestSteps.DefaultPipeline());
        }
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

    private class TestSkillProvider : ISkillProvider
    {
        public Task<Skill> LoadAsync(string skillName, CancellationToken ct = default)
        {
            var skills = TestSteps.DefaultSkills().ToDictionary(s => s.Name);
            return Task.FromResult(skills[skillName]);
        }
    }
}
