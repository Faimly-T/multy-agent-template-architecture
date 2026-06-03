namespace AgentFramework.Core.Agent.Handlers;

/// <summary>
/// The content of a single handler turn: what the handler received and what it produced.
///
/// In Denis Rothman's context-engineering model, each handler in a chain compresses
/// all prior reasoning into its <see cref="Input"/> (the accumulated context window) and
/// contributes a new <see cref="Output"/> that becomes the next handler's context seed.
/// This deliberate compression prevents context-window overflow in long chains.
/// </summary>
/// <param name="Input">
/// The context this handler consumed — typically the previous handler's output,
/// obtained via <see cref="HandlerContextExtensions.LastOutput"/>.
/// </param>
/// <param name="Output">
/// The text or JSON this handler produced. For LLM-backed handlers this is the model's
/// raw response; for non-LLM handlers it is a formatted summary assembled from session state.
/// </param>
public record HandlerContent(string Input, string Output);

/// <summary>
/// Provenance metadata for a handler turn — used for tracing and debugging.
/// </summary>
/// <param name="RequestedBy">
/// The name of the upstream handler or orchestrator that triggered this one, or <c>null</c>
/// when this is the first handler in the chain.
/// </param>
/// <param name="IsLlmCall">
/// <c>true</c> when an LLM was invoked; <c>false</c> for pure context-assembly handlers
/// (e.g. <c>CaptureObjectiveContextCommand</c>, <c>OrganizeSynthesisHandler</c>).
/// Used downstream to calculate accurate token attribution.
/// </param>
public record HandlerMetadata(string? RequestedBy, bool IsLlmCall);

/// <summary>
/// An immutable record of one handler's execution within a <see cref="IStepChain"/>.
///
/// The chain accumulates these records into a journal (<c>IReadOnlyList&lt;HandlerExchange&gt;</c>)
/// that represents the full context history of the step. Later handlers read prior exchanges
/// via <see cref="HandlerContextExtensions"/> to thread prior reasoning into their prompts.
/// </summary>
/// <param name="Sender">
/// The simple class name of the handler that produced this exchange
/// (e.g. <c>"CheckpointValidatorCommand"</c>). Used as the lookup key in
/// <see cref="HandlerContextExtensions.GetOutput"/>.
/// </param>
/// <param name="Content">The input consumed and output produced by this handler.</param>
/// <param name="Metadata">Tracing metadata — provenance and LLM call flag.</param>
public record HandlerExchange(string Sender, HandlerContent Content, HandlerMetadata Metadata);
