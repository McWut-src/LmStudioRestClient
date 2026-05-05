namespace LmStudioRestClient;

/// <summary>
/// Represents a single line in the JSONL input file.
/// </summary>
public sealed record PromptItem(string Prompt, bool Completed = false);

/// <summary>
/// Response from GET /api/v1/models
/// </summary>
public sealed record ModelsResponse(ModelInfo[] Data);

public sealed record ModelInfo(string Id, string? Publisher, string? Path);

/// <summary>
/// Request payload for POST /api/v1/chat
/// </summary>
public sealed record ChatRequest(string Model, string Input, bool Stream = false, string[]? Integrations = null);

public sealed record ChatMessage(string Role, string Content);

/// <summary>
/// Normalized response from POST /api/v1/chat (non-streaming)
/// </summary>
public sealed record ChatResponse(string? Id, string? Text, int ToolCallCount = 0);
