using GameFrameX.SuperSocket.Server.Abstractions;

namespace GameFrameX.SuperSocket.WebSocket.Server;

/// <summary>
/// Defines a handler for WebSocket sub-protocols.
/// </summary>
interface ISubProtocolHandler : IPackageHandler<WebSocketPackage>
{
    /// <summary>
    /// Gets the name of the sub-protocol.
    /// </summary>
    string Name { get; }
}