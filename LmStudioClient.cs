using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;

namespace LmStudioRestClient;

public sealed class LmStudioClient : ILmStudioClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // ── DI path: called by IHttpClientFactory ─────────────────────────────────
    // HttpClient is already configured (BaseAddress, Timeout, Auth) by AddCoreClient.
    public LmStudioClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _ownsHttpClient = false;
    }

    // ── Standalone factory: no DI required ───────────────────────────────────
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

                var parsed = ParseModelsResponse(body);
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

        return ParseChatResponse(body);
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

        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(ct);
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

            var delta = ExtractDeltaText(jsonPart);
            if (!string.IsNullOrEmpty(delta))
            {
                yield return delta;
            }
        }
    }

    // ── Private parsers ───────────────────────────────────────────────────────

    private static string? ExtractDeltaText(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // OpenAI-compatible: choices[0].delta.content
            if (root.TryGetProperty("choices", out var choices) &&
                choices.ValueKind == JsonValueKind.Array)
            {
                foreach (var choice in choices.EnumerateArray())
                {
                    if (choice.TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("content", out var content) &&
                        content.ValueKind == JsonValueKind.String)
                    {
                        return content.GetString();
                    }
                }
            }

            // LM Studio streaming delta: {"type":"reasoning.delta"|"message.delta","content":"..."}
            if (root.TryGetProperty("type", out var typeEl) &&
                typeEl.ValueKind == JsonValueKind.String &&
                root.TryGetProperty("content", out var rootContent) &&
                rootContent.ValueKind == JsonValueKind.String)
            {
                return rootContent.GetString();
            }

            // LM Studio native: output[*].content[*].text  or  output[*].text
            if (root.TryGetProperty("output", out var output) &&
                output.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in output.EnumerateArray())
                {
                    if (item.TryGetProperty("content", out var contentArray) &&
                        contentArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var contentItem in contentArray.EnumerateArray())
                        {
                            if (contentItem.TryGetProperty("text", out var text) &&
                                text.ValueKind == JsonValueKind.String)
                            {
                                var value = text.GetString();
                                if (!string.IsNullOrEmpty(value))
                                    return value;
                            }
                        }
                    }

                    if (item.TryGetProperty("text", out var directText) &&
                        directText.ValueKind == JsonValueKind.String)
                    {
                        return directText.GetString();
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static ChatResponse ParseChatResponse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var id = root.TryGetProperty("id", out var idElement)
            ? idElement.GetString()
            : null;

        int toolCallCount = 0;
        string? messageText = null;

        // LM Studio native format: output array
        if (root.TryGetProperty("output", out var outputElement) &&
            outputElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in outputElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                if (item.TryGetProperty("content", out var contentElement) &&
                    contentElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var contentItem in contentElement.EnumerateArray())
                    {
                        if (contentItem.ValueKind != JsonValueKind.Object)
                            continue;

                        if (contentItem.TryGetProperty("text", out var textElement))
                        {
                            var text = textElement.GetString();
                            if (!string.IsNullOrWhiteSpace(text))
                                messageText = text;
                        }
                    }
                }

                if (item.TryGetProperty("type", out var typeElement))
                {
                    var itemType = typeElement.GetString();

                    if (string.Equals(itemType, "tool_call", StringComparison.OrdinalIgnoreCase))
                    {
                        toolCallCount++;
                    }

                    if (string.Equals(itemType, "message", StringComparison.OrdinalIgnoreCase) &&
                        item.TryGetProperty("content", out var messageContent) &&
                        messageContent.ValueKind == JsonValueKind.String)
                    {
                        var text = messageContent.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                            messageText = text;
                    }
                }

                if (item.TryGetProperty("text", out var directTextElement))
                {
                    var text = directTextElement.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        messageText = text;
                }
            }

            return new ChatResponse(id, messageText, toolCallCount);
        }

        // OpenAI-compatible format: choices array
        if (root.TryGetProperty("choices", out var choicesElement) &&
            choicesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var choice in choicesElement.EnumerateArray())
            {
                if (choice.ValueKind != JsonValueKind.Object)
                    continue;

                if (choice.TryGetProperty("message", out var messageElement) &&
                    messageElement.ValueKind == JsonValueKind.Object &&
                    messageElement.TryGetProperty("content", out var content) &&
                    content.ValueKind == JsonValueKind.String)
                {
                    var text = content.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return new ChatResponse(id, text);
                }
            }
        }

        // Flat format: root.text
        if (root.TryGetProperty("text", out var textRootElement) &&
            textRootElement.ValueKind == JsonValueKind.String)
        {
            return new ChatResponse(id, textRootElement.GetString());
        }

        return new ChatResponse(id, null);
    }

    private static ModelsResponse? ParseModelsResponse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("data", out var dataElement) &&
            dataElement.ValueKind == JsonValueKind.Array)
        {
            return new ModelsResponse(ParseModelArray(dataElement));
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            return new ModelsResponse(ParseModelArray(root));
        }

        return null;
    }

    private static ModelInfo[] ParseModelArray(JsonElement arrayElement)
    {
        var models = new List<ModelInfo>();

        foreach (var item in arrayElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var id = item.TryGetProperty("id", out var idElement)
                ? idElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(id))
                continue;

            var publisher = item.TryGetProperty("publisher", out var publisherElement)
                ? publisherElement.GetString()
                : null;

            var path = item.TryGetProperty("path", out var pathElement)
                ? pathElement.GetString()
                : null;

            models.Add(new ModelInfo(id, publisher, path));
        }

        return models.ToArray();
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}