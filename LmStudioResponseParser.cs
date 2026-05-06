using System.Text.Json;

namespace LmStudioRestClient;

/// <summary>
/// Provides static helper methods for parsing LM Studio API responses.
/// </summary>
internal static class LmStudioResponseParser
{
    /// <summary>
    /// Extracts a text delta from a streaming or non-streaming JSON response.
    /// Handles OpenAI-compatible, LM Studio streaming, and native formats.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>The extracted text delta, or <c>null</c> if not found or on error.</returns>
    internal static string? ExtractDeltaText(string json)
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


    /// <summary>
    /// Parses a chat response JSON string and extracts the message and tool call count.
    /// Handles LM Studio native, OpenAI-compatible, and flat formats.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>A <see cref="ChatResponse"/> object containing the parsed data.</returns>
    internal static ChatResponse ParseChatResponse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Try to get response_id first (LM Studio native), fallback to id (OpenAI-compatible)
        string? responseId = null;
        if (root.TryGetProperty("response_id", out var responseIdElement))
        {
            responseId = responseIdElement.GetString();
        }
        else if (root.TryGetProperty("id", out var idElement))
        {
            responseId = idElement.GetString();
        }

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

            return new ChatResponse(responseId, messageText, toolCallCount);
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
                        return new ChatResponse(responseId, text);
                }
            }
        }

        // Flat format: root.text
        if (root.TryGetProperty("text", out var textRootElement) &&
            textRootElement.ValueKind == JsonValueKind.String)
        {
            return new ChatResponse(responseId, textRootElement.GetString());
        }

        return new ChatResponse(responseId, null);
    }


    /// <summary>
    /// Parses a models response JSON string and extracts the list of models.
    /// Handles both object and array root formats.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>A <see cref="ModelsResponse"/> object containing the parsed models, or <c>null</c> if parsing fails.</returns>
    internal static ModelsResponse? ParseModelsResponse(string json)
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

    /// <summary>
    /// Parses a JSON array of model objects and returns an array of <see cref="ModelInfo"/>.
    /// </summary>
    /// <param name="arrayElement">The <see cref="JsonElement"/> representing the array of models.</param>
    /// <returns>An array of <see cref="ModelInfo"/> parsed from the JSON array.</returns>
    internal static ModelInfo[] ParseModelArray(JsonElement arrayElement)
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

        return [.. models];
    }
}
