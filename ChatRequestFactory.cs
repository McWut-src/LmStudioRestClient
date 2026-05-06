using System.Diagnostics.CodeAnalysis;

namespace LmStudioRestClient;

/// <summary>
/// Factory for creating <see cref="ChatRequest"/> and <see cref="IChatRequest"/> instances for common scenarios.
/// </summary>
public static class ChatRequestFactory
{
    /// <summary>
    /// Creates a new stateful chat request.
    /// </summary>
    public static ChatRequest New(string model, string input, string[]? integrations = null)
        => new ChatRequest { Model = model, Input = input, Store = true, Integrations = integrations };

    /// <summary>
    /// Creates a chat request to continue a conversation.
    /// </summary>
    public static ChatRequest Continue(string model, string input, string previousResponseId, string[]? integrations = null)
        => new ChatRequest { Model = model, Input = input, PreviousResponseId = previousResponseId, Store = true, Integrations = integrations };

    /// <summary>
    /// Creates a stateless chat request (not stored on server).
    /// </summary>
    public static ChatRequest Stateless(string model, string input, string[]? integrations = null)
        => new ChatRequest { Model = model, Input = input, Store = false, Integrations = integrations };

    /// <summary>
    /// Creates a streaming chat request (stateful by default).
    /// </summary>
    public static ChatRequest Streaming(string model, string input, bool stateful = true, string? previousResponseId = null, string[]? integrations = null)
        => new ChatRequest { Model = model, Input = input, Stream = true, Store = stateful ? true : false, PreviousResponseId = previousResponseId, Integrations = integrations };

    /// <summary>
    /// Creates a custom chat request for advanced scenarios.
    /// </summary>
    public static ChatRequest Custom(
        string model,
        string input,
        bool stream = false,
        string[]? integrations = null,
        string? previousResponseId = null,
        bool? store = null)
        => new ChatRequest
        {
            Model = model,
            Input = input,
            Stream = stream,
            Integrations = integrations,
            PreviousResponseId = previousResponseId,
            Store = store
        };
}
