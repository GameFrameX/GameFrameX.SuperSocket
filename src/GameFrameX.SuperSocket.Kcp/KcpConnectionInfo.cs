using System.Net;
using System.Net.Sockets;
using GameFrameX.SuperSocket.Connection;

namespace GameFrameX.SuperSocket.Kcp;

/// <summary>
/// KCP 连接创建所需信息。
/// </summary>
internal struct KcpConnectionInfo
{
    /// <summary>
    /// 获取或设置关联的 Socket。
    /// </summary>
    public Socket Socket { get; set; }

    /// <summary>
    /// 获取或设置远端地址。
    /// </summary>
    public IPEndPoint RemoteEndPoint { get; set; }

    /// <summary>
    /// 获取或设置 KCP 会话的 Conv。
    /// </summary>
    public uint Conv { get; set; }

    /// <summary>
    /// 获取或设置会话标识。
    /// </summary>
    public string SessionIdentifier { get; set; }

    /// <summary>
    /// 获取或设置连接选项。
    /// </summary>
    public ConnectionOptions ConnectionOptions { get; set; }

    /// <summary>
    /// 获取或设置 KCP 连接选项。
    /// </summary>
    public KcpConnectionOptions KcpOptions { get; set; }
}