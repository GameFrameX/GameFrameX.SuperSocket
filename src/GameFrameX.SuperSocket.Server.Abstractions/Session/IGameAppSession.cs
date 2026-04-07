using System.Buffers;

namespace GameFrameX.SuperSocket.Server.Abstractions.Session;

/// <summary>
/// 
/// </summary>
public interface IGameAppSession
{
    /// <summary>
    /// Session unique Id   
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Session is connected
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Send data to client
    /// </summary>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask SendAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send data to client
    /// </summary>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a sequence of binary data asynchronously using the connection.
    /// </summary>
    /// <param name="data">The sequence of binary data to send.</param>
    /// <param name="cancellationToken">The token for canceling the operation.</param>
    /// <returns>A task representing the asynchronous send operation.</returns>
    ValueTask SendAsync(ReadOnlySequence<byte> data, CancellationToken cancellationToken = default);
}