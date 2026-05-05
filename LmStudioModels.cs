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
public sealed record ChatRequest(string Model, string Input, bool Stream = false, string[]? Integrations = null);

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
public sealed record ChatResponse(string? Id, string? Text, int ToolCallCount = 0);
