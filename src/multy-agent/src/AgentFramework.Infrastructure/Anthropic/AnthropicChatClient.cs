using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentFramework.Core.Agent.Conversation;
using AgentFramework.Core.Agent.Ports;
using AgentFramework.Core.Agent.Steps;
using Microsoft.Extensions.Options;

namespace AgentFramework.Infrastructure.Anthropic;

public sealed class AnthropicChatClient : IChatClient, IDisposable
{
    private readonly HttpClient _http;
    private readonly AnthropicOptions _options;
    private readonly bool _ownsHttpClient;

    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>DI constructor — used by AddHttpClient registration.</summary>
    public AnthropicChatClient(IOptions<AnthropicOptions> options, HttpClient httpClient)
        : this(options.Value, httpClient, ownsHttpClient: false)
    {
    }

    /// <summary>Manual constructor — creates and owns its own HttpClient.</summary>
    public AnthropicChatClient(AnthropicOptions options)
        : this(options, new HttpClient(), ownsHttpClient: true)
    {
    }

    private AnthropicChatClient(AnthropicOptions options, HttpClient httpClient, bool ownsHttpClient)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsHttpClient = ownsHttpClient;
        _http.BaseAddress ??= _options.BaseUrl;
        _http.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", _options.ApiKey);
        _http.DefaultRequestHeaders.TryAddWithoutValidation("anthropic-version", _options.ApiVersion);
    }

    public async Task<StepResult> SendAsync(
        IReadOnlyList<ChatMessage> messages, AgentStep step, CancellationToken ct = default)
    {
        var (systemPrompt, apiMessages) = MapMessages(messages);

        var request = new AnthropicRequest
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            System = systemPrompt,
            Messages = apiMessages
        };

        var json = JsonSerializer.Serialize(request, SerializeOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync("messages", content, ct);

        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Anthropic API returned {(int)response.StatusCode}: {body}");

        var apiResponse = JsonSerializer.Deserialize<AnthropicResponse>(body, SerializeOptions)
            ?? throw new InvalidOperationException("Failed to deserialize Anthropic response.");

        var rawText = ExtractText(apiResponse);
        var jsonBlock = ExtractJsonBlock(rawText);

        return StepResultParser.Parse(jsonBlock, rawText, step);
    }

    private static (string? system, List<ApiMessage> messages) MapMessages(
        IReadOnlyList<ChatMessage> messages)
    {
        string? system = null;
        var apiMessages = new List<ApiMessage>();

        foreach (var msg in messages)
        {
            if (msg.Role == MessageRole.System)
            {
                system = msg.Content;
                continue;
            }

            apiMessages.Add(new ApiMessage
            {
                Role = msg.Role == MessageRole.Assistant ? "assistant" : "user",
                Content = msg.Content
            });
        }

        return (system, apiMessages);
    }

    private static string ExtractText(AnthropicResponse response)
    {
        var textBlock = response.Content?.FirstOrDefault(c => c.Type == "text");
        return textBlock?.Text ?? string.Empty;
    }

    internal static string ExtractJsonBlock(string text)
    {
        var fenceStart = text.IndexOf("```json", StringComparison.Ordinal);
        if (fenceStart >= 0)
        {
            var jsonStart = text.IndexOf('\n', fenceStart) + 1;
            var fenceEnd = text.IndexOf("```", jsonStart, StringComparison.Ordinal);
            if (fenceEnd > jsonStart)
                return text[jsonStart..fenceEnd].Trim();
        }

        var braceStart = text.IndexOf('{');
        var braceEnd = text.LastIndexOf('}');
        if (braceStart >= 0 && braceEnd > braceStart)
            return text[braceStart..(braceEnd + 1)];

        return text;
    }

    public void Dispose()
    {
        if (_ownsHttpClient) _http.Dispose();
    }

    #region Anthropic API DTOs

    private sealed class AnthropicRequest
    {
        public string Model { get; init; } = default!;
        public int MaxTokens { get; init; }
        public string? System { get; init; }
        public List<ApiMessage> Messages { get; init; } = [];
    }

    private sealed class ApiMessage
    {
        public string Role { get; init; } = default!;
        public string Content { get; init; } = default!;
    }

    private sealed class AnthropicResponse
    {
        public List<ContentBlock>? Content { get; init; }
        public UsageBlock? Usage { get; init; }
    }

    private sealed class ContentBlock
    {
        public string Type { get; init; } = default!;
        public string Text { get; init; } = default!;
    }

    private sealed class UsageBlock
    {
        public int InputTokens { get; init; }
        public int OutputTokens { get; init; }
    }

    #endregion
}
