using AgentFramework.Core.Agent.Ports;
using AgentFramework.Infrastructure.Anthropic;
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

        return services;
    }
}
