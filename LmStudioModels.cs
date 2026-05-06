using System.Text.Json.Serialization;

namespace LmStudioRestClient;

/// <summary>
/// Represents a single line in the JSONL input file.
/// </summary>
public sealed record PromptItem(string Prompt, bool Completed = false);

/// <summary>
/// Response from GET /api/v1/models
/// </summary>
public sealed record ModelsResponse(ModelInfo[] Data);

/// <summary>
/// Represents information about a model.
/// </summary>
public sealed record ModelInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelInfo"/> record.
    /// </summary>
    /// <param name="Id">The model identifier.</param>
    /// <param name="Publisher">The publisher of the model.</param>
    /// <param name="Path">The path to the model.</param>
    public ModelInfo(string Id, string? Publisher, string? Path)
    {
        this.Id = Id;
        this.Publisher = Publisher;
        this.Path = Path;
    }

    /// <summary>
    /// Gets the model identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the publisher of the model.
    /// </summary>
    public string? Publisher { get; }

    /// <summary>
    /// Gets the path to the model.
    /// </summary>
    public string? Path { get; }
}

/// <summary>
/// Request payload for POST /api/v1/chat
/// </summary>
public sealed record ChatRequest : IChatRequest
{
    /// <summary>
    /// Gets the model ID to use for the chat request.
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>
    /// Gets the user prompt or message to send.
    /// </summary>
    [JsonPropertyName("input")]
    public required string Input { get; init; }

    /// <summary>
    /// Gets a value indicating whether to enable streaming (SSE) for the response.
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; init; } = false;

    /// <summary>
    /// Gets the optional integrations to enable.
    /// </summary>
    [JsonPropertyName("integrations")]
    public string[]? Integrations { get; init; }

    /// <summary>
    /// Gets the response_id from a previous request to continue a conversation.
    /// </summary>
    [JsonPropertyName("previous_response_id")]
    public string? PreviousResponseId { get; init; }

    /// <summary>
    /// Gets a value indicating whether to store the conversation for stateful chat. Set to false for stateless requests.
    /// </summary>
    [JsonPropertyName("store")]
    public bool? Store { get; init; }
}

/// <summary>
/// Represents a chat message.
/// </summary>
public sealed record ChatMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessage"/> record.
    /// </summary>
    /// <param name="Role">The role of the message sender.</param>
    /// <param name="Content">The content of the message.</param>
    public ChatMessage(string Role, string Content)
    {
        this.Role = Role;
        this.Content = Content;
    }

    /// <summary>
    /// Gets the role of the message sender.
    /// </summary>
    public string Role { get; }

    /// <summary>
    /// Gets the content of the message.
    /// </summary>
    public string Content { get; }
}

/// <summary>
/// Normalized response from POST /api/v1/chat (non-streaming)
/// </summary>
/// <param name="ResponseId">The unique response_id returned by LM Studio for conversation continuation.</param>
/// <param name="Text">The generated text response from the model.</param>
/// <param name="ToolCallCount">The number of tool calls made during the response generation.</param>
public sealed record ChatResponse(string? ResponseId, string? Text, int ToolCallCount = 0);
