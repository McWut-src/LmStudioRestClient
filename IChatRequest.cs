namespace LmStudioRestClient;

/// <summary>
/// Interface for chat request payloads.
/// </summary>
public interface IChatRequest
{
    string Model { get; }
    string Input { get; }
    bool Stream { get; }
    string[]? Integrations { get; }
    string? PreviousResponseId { get; }
    bool? Store { get; }
}
