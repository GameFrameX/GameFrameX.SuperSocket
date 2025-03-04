using GameFrameX.SuperSocket.Server.Abstractions.Session;

namespace GameFrameX.SuperSocket.WebSocket.Server;

/// <summary>
/// WebSocket 子协议处理器基类
/// </summary>
internal abstract class SubProtocolHandlerBase : ISubProtocolHandler
{
    /// <summary>
    /// 初始化 WebSocket 子协议处理器基类的新实例
    /// </summary>
    /// <param name="name">子协议名称</param>
    public SubProtocolHandlerBase(string name)
    {
        Name = name;
    }

    /// <summary>
    /// 获取子协议名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 处理 WebSocket 数据包
    /// </summary>
    /// <param name="session">应用会话</param>
    /// <param name="package">WebSocket 数据包</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public abstract ValueTask Handle(IAppSession session, WebSocketPackage package, CancellationToken cancellationToken);
}