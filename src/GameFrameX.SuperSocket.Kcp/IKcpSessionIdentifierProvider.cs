using System.Net;

namespace GameFrameX.SuperSocket.Kcp;

/// <summary>
/// KCP 会话标识提供者。
/// 从 UDP 包中提取标识信息（如 Conv），用于路由到正确的连接。
/// </summary>
public interface IKcpSessionIdentifierProvider
{
    /// <summary>
    /// 从收到的 UDP 包中提取会话标识。
    /// </summary>
    /// <param name="remoteEndPoint">远端地址。使用 EndPoint 基类，允许自定义实现同时支持 IP、域名或其它 Socket 端点。</param>
    /// <param name="data">UDP 包原始数据（至少包含 KCP 头部 24 字节）</param>
    /// <returns>会话唯一标识</returns>
    string GetSessionIdentifier(EndPoint remoteEndPoint, ReadOnlySpan<byte> data);
}
