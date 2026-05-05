# LmStudioRestClient

[![NuGet](https://img.shields.io/nuget/v/LmStudioRestClient.svg)](https://www.nuget.org/packages/LmStudioRestClient/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/LmStudioRestClient.svg)](https://www.nuget.org/packages/LmStudioRestClient/)

A lightweight, modern .NET HTTP client for [LM Studio](https://lmstudio.ai/) with full dependency injection support. Easily integrate local LLMs into your .NET applications with support for both streaming and non-streaming chat completions.

## Features

- **🚀 Easy Integration**: Simple setup with dependency injection or standalone usage
- **🔄 Streaming Support**: Real-time streaming chat completions via Server-Sent Events (SSE)
- **🎯 Type-Safe**: Strongly-typed request/response models with nullable reference types
- **🔌 Flexible Configuration**: Configure via `appsettings.json`, code, or both
- **🏭 Production-Ready**: Built on `IHttpClientFactory` for optimal performance and resource management
- **🌐 API Compatibility**: Works with both LM Studio native and OpenAI-compatible endpoints
- **⚡ Modern .NET**: Targets .NET 10 with latest C# features

## Supported .NET Versions

- **.NET 10.0** and later

This library uses the latest C# language features and follows modern .NET best practices.

## Installation

### Via NuGet Package Manager Console

```powershell
Install-Package LmStudioRestClient
```

### Via .NET CLI

```bash
dotnet add package LmStudioRestClient
```

### Via PackageReference

```xml
<PackageReference Include="LmStudioRestClient" Version="1.0.0" />
```

## Quick Start

### Option 1: Dependency Injection (Recommended)

#### Configure in `appsettings.json`

```json
{
  "LmStudio": {
    "BaseUrl": "http://127.0.0.1:1234",
    "ApiToken": null,
    "Timeout": "00:10:00"
  }
}
```

#### Register in `Program.cs`

```csharp
using LmStudioRestClient;

var builder = WebApplication.CreateBuilder(args);

// Register client with configuration from appsettings.json
builder.Services.AddLmStudioClient(builder.Configuration);

var app = builder.Build();
```

#### Use in Your Services

```csharp
public class MyService
{
    private readonly ILmStudioClient _client;

    public MyService(ILmStudioClient client)
    {
        _client = client;
    }

    public async Task<string> AskQuestionAsync(string question)
    {
        var request = new ChatRequest(
            Model: "llama-3.2-1b-instruct",
            Input: question,
            Stream: false
        );

        var response = await _client.ChatAsync(request);
        return response?.Text ?? "No response";
    }
}
```

### Option 2: Standalone Usage (No DI)

```csharp
using LmStudioRestClient;

// Create client directly
var client = LmStudioClient.Create(
    baseUrl: "http://127.0.0.1:1234",
    apiToken: null,
    timeout: TimeSpan.FromMinutes(10)
);

// Use the client
var request = new ChatRequest(
    Model: "llama-3.2-1b-instruct",
    Input: "What is the capital of France?",
    Stream: false
);

var response = await client.ChatAsync(request);
Console.WriteLine(response?.Text);

// Don't forget to dispose
client.Dispose();
```

## Configuration

### Configuration Options

The `LmStudioClientOptions` class provides the following configuration properties:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `BaseUrl` | `string` | `http://127.0.0.1:1234` | Base URL of your LM Studio server |
| `ApiToken` | `string?` | `null` | Optional Bearer token for authentication |
| `Timeout` | `TimeSpan` | `00:10:00` | Request timeout (increase for large models or slow hardware) |

### Configuration via `appsettings.json`

```json
{
  "LmStudio": {
    "BaseUrl": "http://127.0.0.1:1234",
    "ApiToken": "your-optional-token-here",
    "Timeout": "00:15:00"
  }
}
```

```csharp
// In Program.cs
builder.Services.AddLmStudioClient(builder.Configuration);
```

### Programmatic Configuration

```csharp
// In Program.cs
builder.Services.AddLmStudioClient(options =>
{
    options.BaseUrl = "http://127.0.0.1:1234";
    options.ApiToken = "your-optional-token-here";
    options.Timeout = TimeSpan.FromMinutes(15);
});
```

### Hybrid Configuration (Config File + Code Overrides)

```csharp
// In Program.cs
builder.Services.AddLmStudioClient(
    builder.Configuration,
    options =>
    {
        // Override specific settings from appsettings.json
        options.Timeout = TimeSpan.FromMinutes(20);
    }
);
```

## API Reference

### Client Interface: `ILmStudioClient`

The main interface provides three methods for interacting with LM Studio:

#### `ListModelsAsync`

List all available models in your LM Studio instance.

```csharp
Task<ModelsResponse?> ListModelsAsync(CancellationToken ct = default);
```

**Example:**

```csharp
var models = await client.ListModelsAsync();
if (models?.Data != null)
{
    foreach (var model in models.Data)
    {
        Console.WriteLine($"Model: {model.Id}");
        Console.WriteLine($"  Publisher: {model.Publisher}");
        Console.WriteLine($"  Path: {model.Path}");
    }
}
```

**LM Studio Endpoints:**
- Primary: `GET /api/v1/models`
- Fallback: `GET /v1/models` (OpenAI-compatible)

#### `ChatAsync`

Send a non-streaming chat request and await the complete response.

```csharp
Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken ct = default);
```

**Example:**

```csharp
var request = new ChatRequest(
    Model: "llama-3.2-1b-instruct",
    Input: "Explain quantum computing in simple terms.",
    Stream: false
);

var response = await client.ChatAsync(request);
Console.WriteLine(response?.Text);
```

**LM Studio Endpoint:**
- `POST /api/v1/chat`

#### `StreamingChatAsync`

Send a streaming chat request and receive text deltas in real-time via Server-Sent Events (SSE).

```csharp
IAsyncEnumerable<string> StreamingChatAsync(ChatRequest request, CancellationToken ct = default);
```

**Example:**

```csharp
var request = new ChatRequest(
    Model: "llama-3.2-1b-instruct",
    Input: "Write a short story about a robot.",
    Stream: true
);

await foreach (var delta in client.StreamingChatAsync(request))
{
    Console.Write(delta); // Print each chunk as it arrives
}
```

**LM Studio Endpoint:**
- `POST /api/v1/chat` (with streaming enabled)

### Models

#### `ChatRequest`

Request payload for chat completions.

```csharp
public sealed record ChatRequest(
    string Model,           // Model ID (e.g., "llama-3.2-1b-instruct")
    string Input,           // User prompt or message
    bool Stream = false,    // Enable streaming (SSE)
    string[]? Integrations = null  // Optional integrations
);
```

#### `ChatResponse`

Response from non-streaming chat completions.

```csharp
public sealed record ChatResponse(
    string? Id,             // Response ID
    string? Text,           // Generated text
    int ToolCallCount = 0   // Number of tool calls (if applicable)
);
```

#### `ModelsResponse`

Response from the models endpoint.

```csharp
public sealed record ModelsResponse(
    ModelInfo[] Data        // Array of available models
);
```

#### `ModelInfo`

Information about a single model.

```csharp
public sealed record ModelInfo(
    string Id,              // Model identifier
    string? Publisher,      // Model publisher
    string? Path            // Local path to model
);
```

#### `PromptItem`

Represents a single prompt item (for batch processing scenarios).

```csharp
public sealed record PromptItem(
    string Prompt,          // The prompt text
    bool Completed = false  // Whether processing is complete
);
```

## Advanced Examples

### Streaming with Progress Indicator

```csharp
var request = new ChatRequest(
    Model: "llama-3.2-1b-instruct",
    Input: "Write a haiku about programming.",
    Stream: true
);

Console.WriteLine("Generating response...\n");

await foreach (var delta in client.StreamingChatAsync(request))
{
    Console.Write(delta);
}

Console.WriteLine("\n\nDone!");
```

### Error Handling

```csharp
try
{
    var response = await client.ChatAsync(request);
    if (response?.Text != null)
    {
        Console.WriteLine(response.Text);
    }
    else
    {
        Console.WriteLine("No response received");
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"HTTP error: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Operation error: {ex.Message}");
}
```

### Using Different Models

```csharp
// First, list available models
var models = await client.ListModelsAsync();

if (models?.Data.Length > 0)
{
    // Use the first available model
    var modelId = models.Data[0].Id;

    var request = new ChatRequest(
        Model: modelId,
        Input: "Hello, world!",
        Stream: false
    );

    var response = await client.ChatAsync(request);
    Console.WriteLine(response?.Text);
}
```

### Cancellation Token Support

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var response = await client.ChatAsync(request, cts.Token);
    Console.WriteLine(response?.Text);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Request was cancelled");
}
```

## LM Studio API Endpoints

This client interacts with the following LM Studio API endpoints:

| Method | Endpoint | Description | Client Method |
|--------|----------|-------------|---------------|
| `GET` | `/api/v1/models` | List available models (native) | `ListModelsAsync()` |
| `GET` | `/v1/models` | List available models (OpenAI-compatible fallback) | `ListModelsAsync()` |
| `POST` | `/api/v1/chat` | Chat completion (non-streaming) | `ChatAsync()` |
| `POST` | `/api/v1/chat` | Chat completion (streaming via SSE) | `StreamingChatAsync()` |

The client automatically handles format differences between LM Studio's native API and OpenAI-compatible endpoints.

## Contributing

Contributions are welcome! Here's how you can help:

### Reporting Issues

- Use the [GitHub Issues](https://github.com/yourusername/LmStudioRestClient/issues) page
- Search existing issues before creating a new one
- Provide clear reproduction steps and environment details
- Include relevant code snippets and error messages

### Submitting Pull Requests

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Development Guidelines

- Follow the existing code style and conventions
- Ensure all tests pass before submitting
- Add tests for new features
- Update documentation for API changes
- Keep commits atomic and well-described
- Target .NET 10 and use latest C# features appropriately

### Building Locally

```bash
# Clone the repository
git clone https://github.com/yourusername/LmStudioRestClient.git
cd LmStudioRestClient

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests (if available)
dotnet test
```

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built for [LM Studio](https://lmstudio.ai/) - an excellent desktop application for running local LLMs
- Uses `Microsoft.Extensions.Http` for robust HTTP client management
- Designed with modern .NET dependency injection patterns

## Support

- **Documentation**: [GitHub Wiki](https://github.com/yourusername/LmStudioRestClient/wiki)
- **Issues**: [GitHub Issues](https://github.com/yourusername/LmStudioRestClient/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/LmStudioRestClient/discussions)

---

**Made with ❤️ for the .NET and LLM community**
