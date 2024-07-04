using GameFrameX.SuperSocket.Server.Abstractions.Session;

namespace GameFrameX.SuperSocket.WebSocket.Server;

internal abstract class SubProtocolHandlerBase : ISubProtocolHandler
{
    public SubProtocolHandlerBase(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public abstract ValueTask Handle(IAppSession session, WebSocketPackage package, CancellationToken cancellationToken);
}