using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;

namespace LmStudioRestClient;

/// <summary>
/// Provides methods to interact with the LM Studio REST API.
/// </summary>
public sealed class LmStudioClient : ILmStudioClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };


    /// <summary>
    /// Initializes a new instance of the <see cref="LmStudioClient"/> class using an existing <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    public LmStudioClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _ownsHttpClient = false;
    }

    /// <summary>
    /// Creates a new <see cref="ILmStudioClient"/> instance with the specified configuration.
    /// </summary>
    /// <param name="baseUrl">The base URL of the LM Studio API.</param>
    /// <param name="apiToken">The API token for authentication.</param>
    /// <param name="timeout">The request timeout.</param>
    /// <returns>A new <see cref="ILmStudioClient"/> instance.</returns>
    public static ILmStudioClient Create(
        string baseUrl = "http://127.0.0.1:1234",
        string? apiToken = null,
        TimeSpan? timeout = null)
        => new LmStudioClient(baseUrl, apiToken, timeout);

    private LmStudioClient(string baseUrl, string? apiToken, TimeSpan? timeout)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = timeout ?? TimeSpan.FromMinutes(10)
        };

        if (!string.IsNullOrWhiteSpace(apiToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiToken);
        }

        _ownsHttpClient = true;
    }

    /// <summary>
    /// List all available models in LM Studio.
    /// Tries native endpoint first, then OpenAI-compatible endpoint.
    /// </summary>
    public async Task<ModelsResponse?> ListModelsAsync(CancellationToken ct = default)
    {
        var endpoints = new[] { "/api/v1/models", "/v1/models" };
        var failures = new List<string>();

        foreach (var endpoint in endpoints)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    failures.Add($"{endpoint} => {(int)response.StatusCode} {response.ReasonPhrase}");
                    continue;
                }

                var parsed = LmStudioResponseParser.ParseModelsResponse(body);
                if (parsed?.Data is { Length: > 0 })
                {
                    return parsed;
                }

                failures.Add($"{endpoint} => response parsed but no models returned");
            }
            catch (Exception ex)
            {
                failures.Add($"{endpoint} => {ex.Message}");
            }
        }

        throw new InvalidOperationException(
            $"Could not fetch models from LM Studio. Tried endpoints: {string.Join(" | ", failures)}");
    }

    /// <summary>
    /// POST /api/v1/chat — Send a chat request (non-streaming).
    /// </summary>
    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/v1/chat", request, JsonOptions, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"LM Studio returned {(int)response.StatusCode}: {body}",
                inner: null,
                response.StatusCode);
        }

        return LmStudioResponseParser.ParseChatResponse(body);
    }

    /// <summary>
    /// POST /api/v1/chat — Send a chat request with streaming enabled (SSE).
    /// Yields text deltas as they arrive from the LLM.
    /// </summary>
    public async IAsyncEnumerable<string> StreamingChatAsync(
        ChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/chat");
        requestMessage.Content = JsonContent.Create(request, options: JsonOptions);

        // ResponseHeadersRead: we get the response as soon as headers arrive,
        // then stream the body ourselves. The requestMessage is disposed here but
        // that is safe — the connection stays alive via the response stream.
        using var response = await _httpClient.SendAsync(
            requestMessage, HttpCompletionOption.ResponseHeadersRead, ct);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // SSE lines start with "data: "
            if (!line.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            var jsonPart = line["data: ".Length..].Trim();

            // "[DONE]" signals end of stream
            if (jsonPart == "[DONE]")
            {
                yield break;
            }

            var delta = LmStudioResponseParser.ExtractDeltaText(jsonPart);
            if (!string.IsNullOrEmpty(delta))
            {
                yield return delta;
            }
        }
    }

    /// <summary>
    /// Disposes the underlying <see cref="HttpClient"/> if owned by this instance.
    /// </summary>
    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}