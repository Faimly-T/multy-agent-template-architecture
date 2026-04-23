using AgentFramework.Core.Agent;
using AgentFramework.Core.Agent.Ports;
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
        var agent = new UxPersona(Role.FromMd(markdown), TestSteps.DefaultSteps(), TestSteps.DefaultSkills());

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
        var agent = new UxPersona(Role.FromMd(markdownRole), TestSteps.DefaultSteps(), TestSteps.DefaultSkills());

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
        var agent = new UxPersona(Role.FromMd(markdown), TestSteps.DefaultSteps(), TestSteps.DefaultSkills());

        agent.StartSession(
            "Analyze a college athletic recruiting platform that connects high-school athletes with university scouts.",
            sessionIteration: 1);

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
        Assert.Contains(agent.Session.Checkpoint.SessionObjective, rehydrate.SessionObjective);

        // Conversation should have system + user + assistant messages
        Assert.True(agent.ConversationMessages.Count >= 3);

        // Step index should have advanced
        Assert.Equal(1, agent.Pipeline.CurrentStepIndex);
    }
}
