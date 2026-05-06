namespace LmStudioRestClient;

/// <summary>
/// Extension methods for <see cref="ILmStudioClient"/> to simplify common conversation patterns.
/// </summary>
public static class LmStudioClientExtensions
{
    /// <summary>
    /// Starts a new streaming conversation and yields text deltas in real-time.
    /// </summary>
    /// <param name="client">The LM Studio client.</param>
    /// <param name="model">The model ID to use.</param>
    /// <param name="input">The user prompt or message.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An async stream of response text deltas.</returns>
    public static IAsyncEnumerable<string> StartStreamingConversationAsync(
        this ILmStudioClient client,
        string model,
        string input,
        CancellationToken ct = default)
    {
        var request = new ChatRequest
        {
            Model = model,
            Input = input,
            Stream = true,
            Store = true
        };

        return client.StreamingChatAsync(request, ct);
    }

    /// <summary>
    /// Continues a streaming conversation using the response_id from a previous request.
    /// </summary>
    /// <param name="client">The LM Studio client.</param>
    /// <param name="model">The model ID to use.</param>
    /// <param name="input">The user prompt or message.</param>
    /// <param name="previousResponseId">The response_id from the previous request.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An async stream of response text deltas.</returns>
    public static IAsyncEnumerable<string> ContinueStreamingConversationAsync(
        this ILmStudioClient client,
        string model,
        string input,
        string previousResponseId,
        CancellationToken ct = default)
    {
        var request = new ChatRequest
        {
            Model = model,
            Input = input,
            Stream = true,
            PreviousResponseId = previousResponseId,
            Store = true
        };

        return client.StreamingChatAsync(request, ct);
    }

    /// <summary>
    /// Sends a stateless streaming chat request (conversation is not stored).
    /// </summary>
    /// <param name="client">The LM Studio client.</param>
    /// <param name="model">The model ID to use.</param>
    /// <param name="input">The user prompt or message.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An async stream of response text deltas.</returns>
    public static IAsyncEnumerable<string> StatelessStreamingChatAsync(
        this ILmStudioClient client,
        string model,
        string input,
        CancellationToken ct = default)
    {
        var request = new ChatRequest
        {
            Model = model,
            Input = input,
            Stream = true,
            Store = false
        };

        return client.StreamingChatAsync(request, ct);
    }

    /// <summary>
    /// Builds a <see cref="ChatRequest"/> with fluent API for advanced scenarios.
    /// </summary>
    /// <param name="model">The model ID to use.</param>
    /// <param name="input">The user prompt or message.</param>
    /// <returns>A <see cref="ChatRequestBuilder"/> for fluent configuration.</returns>
    public static ChatRequestBuilder CreateRequest(string model, string input)
    {
        return new ChatRequestBuilder(model, input);
    }
}

/// <summary>
/// Fluent builder for creating <see cref="ChatRequest"/> instances.
/// </summary>
public sealed class ChatRequestBuilder
{
    private readonly string _model;
    private readonly string _input;
    private bool _stream;
    private string[]? _integrations;
    private string? _previousResponseId;
    private bool? _store;

    internal ChatRequestBuilder(string model, string input)
    {
        _model = model;
        _input = input;
    }

    /// <summary>
    /// Enables streaming for the request.
    /// </summary>
    /// <param name="stream">Whether to enable streaming.</param>
    /// <returns>This builder instance for chaining.</returns>
    public ChatRequestBuilder WithStreaming(bool stream = true)
    {
        _stream = stream;
        return this;
    }

    /// <summary>
    /// Sets the integrations for the request.
    /// </summary>
    /// <param name="integrations">The integrations to enable.</param>
    /// <returns>This builder instance for chaining.</returns>
    public ChatRequestBuilder WithIntegrations(params string[] integrations)
    {
        _integrations = integrations;
        return this;
    }

    /// <summary>
    /// Continues a conversation using the response_id from a previous request.
    /// </summary>
    /// <param name="previousResponseId">The response_id from the previous request.</param>
    /// <returns>This builder instance for chaining.</returns>
    public ChatRequestBuilder ContinueFrom(string previousResponseId)
    {
        _previousResponseId = previousResponseId;
        return this;
    }

    /// <summary>
    /// Sets whether to store the conversation for stateful chat.
    /// </summary>
    /// <param name="store">Whether to store the conversation.</param>
    /// <returns>This builder instance for chaining.</returns>
    public ChatRequestBuilder WithStore(bool store)
    {
        _store = store;
        return this;
    }

    /// <summary>
    /// Marks this request as stateless (store = false).
    /// </summary>
    /// <returns>This builder instance for chaining.</returns>
    public ChatRequestBuilder AsStateless()
    {
        _store = false;
        return this;
    }

    /// <summary>
    /// Marks this request as stateful (store = true).
    /// </summary>
    /// <returns>This builder instance for chaining.</returns>
    public ChatRequestBuilder AsStateful()
    {
        _store = true;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="ChatRequest"/> instance.
    /// </summary>
    /// <returns>A configured <see cref="ChatRequest"/>.</returns>
    public ChatRequest Build()
    {
        return new ChatRequest
        {
            Model = _model,
            Input = _input,
            Stream = _stream,
            Integrations = _integrations,
            PreviousResponseId = _previousResponseId,
            Store = _store
        };
    }

    /// <summary>
    /// Implicitly converts the builder to a <see cref="ChatRequest"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    public static implicit operator ChatRequest(ChatRequestBuilder builder) => builder.Build();
}
