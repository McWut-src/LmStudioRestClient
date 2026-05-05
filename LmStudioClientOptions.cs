namespace LmStudioRestClient;

/// <summary>
/// Configuration options for <see cref="LmStudioClient"/>.
/// Bind from appsettings.json under the "LmStudio" section, or configure
/// programmatically via services.AddLmStudioClient(o => { ... }).
/// </summary>
public sealed class LmStudioClientOptions
{
    /// <summary>The section name used when binding from IConfiguration.</summary>
    public const string SectionName = "LmStudio";

    /// <summary>
    /// Base URL of the LM Studio server.
    /// Default: http://127.0.0.1:1234
    /// </summary>
    public string BaseUrl { get; set; } = "http://127.0.0.1:1234";

    /// <summary>
    /// Optional Bearer token sent as the Authorization header.
    /// Leave null/empty if your LM Studio instance has no auth.
    /// </summary>
    public string? ApiToken { get; set; }

    /// <summary>
    /// Maximum time to wait for a single request.
    /// Increase for large models or slow hardware. Default: 10 minutes.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);
}
