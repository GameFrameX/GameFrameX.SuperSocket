using GameFrameX.SuperSocket.Server.Abstractions.Session;

namespace GameFrameX.SuperSocket.WebSocket.Server;

internal class DelegateSubProtocolHandler : SubProtocolHandlerBase
{
    private readonly Func<WebSocketSession, WebSocketPackage, CancellationToken, ValueTask> _packageHandler;

    public DelegateSubProtocolHandler(string name, Func<WebSocketSession, WebSocketPackage, ValueTask> packageHandler)
        : base(name)
    {
        _packageHandler = (session, package, cancellationToken) => packageHandler(session, package);
    }

    public DelegateSubProtocolHandler(string name, Func<WebSocketSession, WebSocketPackage, CancellationToken, ValueTask> packageHandler)
        : base(name)
    {
        _packageHandler = packageHandler;
    }

    public override async ValueTask Handle(IAppSession session, WebSocketPackage package, CancellationToken cancellationToken)
    {
        await _packageHandler(session as WebSocketSession, package, cancellationToken);
    }
}