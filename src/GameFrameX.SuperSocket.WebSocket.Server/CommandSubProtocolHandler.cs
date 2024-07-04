using GameFrameX.SuperSocket.Command;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.Options;

namespace GameFrameX.SuperSocket.WebSocket.Server;

internal sealed class CommandSubProtocolHandler<TPackageInfo> : SubProtocolHandlerBase
{
    private readonly IPackageHandler<WebSocketPackage> _commandMiddleware;

    public CommandSubProtocolHandler(string name, IServiceProvider serviceProvider, IOptions<CommandOptions> commandOptions, IPackageMapper<WebSocketPackage, TPackageInfo> mapper)
        : base(name)
    {
        var keyType = CommandMiddlewareExtensions.GetKeyType<TPackageInfo>();
        var commandMiddlewareType = typeof(WebSocketCommandMiddleware<,>).MakeGenericType(keyType, typeof(TPackageInfo));
        _commandMiddleware = Activator.CreateInstance(commandMiddlewareType, serviceProvider, commandOptions, mapper) as IPackageHandler<WebSocketPackage>;
    }

    public override async ValueTask Handle(IAppSession session, WebSocketPackage package, CancellationToken cancellationToken)
    {
        await _commandMiddleware.Handle(session, package, cancellationToken);
    }
}