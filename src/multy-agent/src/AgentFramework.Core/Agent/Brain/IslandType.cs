namespace AgentFramework.Core.Agent.Session;

/// <summary>
/// Taxonomy of user insights that can be captured during the Capture step.
///
/// Every <see cref="Island"/> is classified by type so the Organize and Distill steps
/// can apply the appropriate lens when grouping and reasoning about users.
/// The types follow a UX research vocabulary (personas, jobs-to-be-done, emotional mapping).
/// </summary>
public enum IslandType
{
    /// <summary>A distinct user archetype — who the system is designed for or against.</summary>
    UserType,

    /// <summary>A desired outcome, aspiration, or job-to-be-done a user wants to achieve.</summary>
    Goal,

    /// <summary>A friction point, barrier, or frustration the user experiences today.</summary>
    PainPoint,

    /// <summary>A repeating action, workaround, or habit observed in the user's context.</summary>
    BehavioralPattern,

    /// <summary>The situation, environment, or conditions in which the user acts (when/where).</summary>
    ContextOfUse,

    /// <summary>A feeling, attitude, or motivational state the user carries into the experience.</summary>
    EmotionalState,

    /// <summary>A user type explicitly outside the target audience — captures who the product should NOT serve.</summary>
    AntiUser,

    /// <summary>An indirect actor who influences or is affected by the user's experience but does not use the product directly.</summary>
    Stakeholder,

    /// <summary>A need or barrier related to assistive technology, motor, sensory, or cognitive accessibility.</summary>
    AccessibilitySignal
}
