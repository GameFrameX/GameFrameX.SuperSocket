using GameFrameX.SuperSocket.Command;
using GameFrameX.SuperSocket.ProtoBase;
using GameFrameX.SuperSocket.Server.Abstractions.Middleware;
using Microsoft.Extensions.Options;

namespace GameFrameX.SuperSocket.WebSocket.Server;

/// <summary>
/// WebSocket 命令中间件接口
/// </summary>
internal interface IWebSocketCommandMiddleware : IMiddleware
{
}

/// <summary>
/// WebSocket 命令中间件实现类
/// </summary>
/// <typeparam name="TKey">命令键类型</typeparam>
/// <typeparam name="TPackageInfo">数据包信息类型</typeparam>
public class WebSocketCommandMiddleware<TKey, TPackageInfo> : CommandMiddleware<TKey, WebSocketPackage, TPackageInfo>, IWebSocketCommandMiddleware
    where TPackageInfo : class, IKeyedPackageInfo<TKey>
{
    /// <summary>
    /// 初始化 WebSocket 命令中间件的新实例
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="commandOptions">命令选项</param>
    public WebSocketCommandMiddleware(IServiceProvider serviceProvider, IOptions<CommandOptions> commandOptions)
        : base(serviceProvider, commandOptions)
    {
    }

    /// <summary>
    /// 初始化 WebSocket 命令中间件的新实例
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="commandOptions">命令选项</param>
    /// <param name="mapper">数据包映射器</param>
    public WebSocketCommandMiddleware(IServiceProvider serviceProvider, IOptions<CommandOptions> commandOptions, IPackageMapper<WebSocketPackage, TPackageInfo> mapper)
        : base(serviceProvider, commandOptions, mapper)
    {
    }
}