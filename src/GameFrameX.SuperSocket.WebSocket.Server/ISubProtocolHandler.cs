using GameFrameX.SuperSocket.Server.Abstractions;

namespace GameFrameX.SuperSocket.WebSocket.Server;

/// <summary>
/// WebSocket 子协议处理器接口
/// </summary>
internal interface ISubProtocolHandler : IPackageHandler<WebSocketPackage>
{
    /// <summary>
    /// 获取子协议名称
    /// </summary>
    string Name { get; }
}