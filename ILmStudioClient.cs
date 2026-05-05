using LmStudioRestClient;

namespace LmStudioRestClient
{
    public interface ILmStudioClient
    {
        Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken ct = default);
        IAsyncEnumerable<string> StreamingChatAsync(ChatRequest request, CancellationToken ct = default);
        Task<ModelsResponse?> ListModelsAsync(CancellationToken ct = default);
    }
}