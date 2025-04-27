namespace GameFrameX.SuperSocket.Server.Abstractions
{
    public interface IServer : IServerInfo, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Starts the server asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous start operation.</returns>
        Task<bool> StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the server asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}