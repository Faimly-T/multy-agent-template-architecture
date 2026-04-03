namespace AgentFramework.Infrastructure.Anthropic;

public sealed class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-4-20250514";
    public int MaxTokens { get; set; } = 4096;
    public string ApiVersion { get; set; } = "2023-06-01";
    public Uri BaseUrl { get; set; } = new("https://api.anthropic.com/v1/");
}
