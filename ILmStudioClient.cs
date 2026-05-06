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
        Task<ChatResponse?> ChatAsync(IChatRequest request, CancellationToken ct = default);

        /// <summary>
        /// Sends a chat request and streams the response as text deltas.
        /// </summary>
        /// <param name="request">The chat request payload.</param>
        /// <param name="ct">A cancellation token.</param>
        /// <returns>An async stream of response text deltas.</returns>
        IAsyncEnumerable<string> StreamingChatAsync(IChatRequest request, CancellationToken ct = default);

        /// <summary>
        /// Lists all available models asynchronously.
        /// </summary>
        /// <param name="ct">A cancellation token.</param>
        /// <returns>The models response.</returns>
        Task<ModelsResponse?> ListModelsAsync(CancellationToken ct = default);

        /// <summary>
        /// Starts a new stateful conversation and returns the response with a response_id for continuation.
        /// </summary>
        /// <param name="model">The model ID to use.</param>
        /// <param name="input">The user prompt or message.</param>
        /// <param name="ct">A cancellation token.</param>
        /// <returns>The chat response with a ResponseId for continuing the conversation.</returns>
        Task<ChatResponse?> StartConversationAsync(string model, string input, CancellationToken ct = default);

        /// <summary>
        /// Continues an existing conversation using the response_id from a previous request.
        /// </summary>
        /// <param name="model">The model ID to use.</param>
        /// <param name="input">The user prompt or message.</param>
        /// <param name="previousResponseId">The response_id from the previous request.</param>
        /// <param name="ct">A cancellation token.</param>
        /// <returns>The chat response with a new ResponseId for further continuation.</returns>
        Task<ChatResponse?> ContinueConversationAsync(string model, string input, string previousResponseId, CancellationToken ct = default);

        /// <summary>
        /// Sends a stateless chat request (conversation is not stored).
        /// </summary>
        /// <param name="model">The model ID to use.</param>
        /// <param name="input">The user prompt or message.</param>
        /// <param name="ct">A cancellation token.</param>
        /// <returns>The chat response without a ResponseId.</returns>
        Task<ChatResponse?> StatelessChatAsync(string model, string input, CancellationToken ct = default);

        /// <summary>
        /// Disposes the client and releases any unmanaged resources.
        /// </summary>
        void Dispose();
    }
}