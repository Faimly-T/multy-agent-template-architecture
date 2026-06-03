using AgentFramework.Core.Agent.Ports;
using AgentFramework.Infrastructure.Anthropic;
using AgentFramework.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFramework.Infrastructure;

public static class DIRegistrations
{
    public static IServiceCollection AddAgentInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<AnthropicOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                configuration.GetSection(AnthropicOptions.SectionName).Bind(options);
            });

        services.AddHttpClient<IChatClient, AnthropicChatClient>();

        // Default to mock publisher — swap with a real broker implementation in production
        services.AddSingleton<IMessagePublisher, MockMessagePublisher>();

        return services;
    }
}
