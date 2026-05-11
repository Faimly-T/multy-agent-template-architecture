using AgentFramework.Core.Agent.Steps;

namespace AgentFramework.Core.Agent.Conversation;

public record TokenUsage(int InputTokens, int OutputTokens);

public record ChatResponse(StepResult Result, TokenUsage Usage);
