namespace LmStudioRestClient
{
    /// <summary>
    /// Defines the contract for interacting with LM Studio REST API.
    /// </summary>
    public interface ILmStudioClient
    {
        /// <summary>
        /// Sends a chat request and returns the response asynchronously.
        /// </summary>
        /// <param name="request">The chat request payload.</param>
        /// <param name="ct">A cancellation token.</param>
        /// <returns>The chat response.</returns>
        Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken ct = default);

        /// <summary>
        /// Sends a chat request and streams the response as text deltas.
        /// </summary>
        /// <param name="request">The chat request payload.</param>
        /// <param name="ct">A cancellation token.</param>
        /// <returns>An async stream of response text deltas.</returns>
        IAsyncEnumerable<string> StreamingChatAsync(ChatRequest request, CancellationToken ct = default);

        /// <summary>
        /// Lists all available models asynchronously.
        /// </summary>
        /// <param name="ct">A cancellation token.</param>
        /// <returns>The models response.</returns>
        Task<ModelsResponse?> ListModelsAsync(CancellationToken ct = default);

        /// <summary>
        /// Disposes the client and releases any unmanaged resources.
        /// </summary>
        void Dispose();
    }
}