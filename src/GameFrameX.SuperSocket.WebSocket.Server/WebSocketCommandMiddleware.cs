using GameFrameX.SuperSocket.Command;
using GameFrameX.SuperSocket.ProtoBase;
using GameFrameX.SuperSocket.Server.Abstractions.Middleware;
using Microsoft.Extensions.Options;

namespace GameFrameX.SuperSocket.WebSocket.Server;

internal interface IWebSocketCommandMiddleware : IMiddleware
{
}

public class WebSocketCommandMiddleware<TKey, TPackageInfo> : CommandMiddleware<TKey, WebSocketPackage, TPackageInfo>, IWebSocketCommandMiddleware
    where TPackageInfo : class, IKeyedPackageInfo<TKey>
{
    public WebSocketCommandMiddleware(IServiceProvider serviceProvider, IOptions<CommandOptions> commandOptions)
        : base(serviceProvider, commandOptions)
    {
    }

    public WebSocketCommandMiddleware(IServiceProvider serviceProvider, IOptions<CommandOptions> commandOptions, IPackageMapper<WebSocketPackage, TPackageInfo> mapper)
        : base(serviceProvider, commandOptions, mapper)
    {
    }
}