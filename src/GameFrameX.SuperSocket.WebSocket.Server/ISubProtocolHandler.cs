using GameFrameX.SuperSocket.Server.Abstractions;

namespace GameFrameX.SuperSocket.WebSocket.Server;

internal interface ISubProtocolHandler : IPackageHandler<WebSocketPackage>
{
    string Name { get; }
}