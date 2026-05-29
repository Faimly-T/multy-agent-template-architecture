namespace AgentFramework.Core.Agent.Steps;

public record CodePipelineConfig(
    StepSkillConfig Kickoff,
    StepSkillConfig Capture,
    StepSkillConfig Organize,
    StepSkillConfig Distill,
    StepSkillConfig Express);
