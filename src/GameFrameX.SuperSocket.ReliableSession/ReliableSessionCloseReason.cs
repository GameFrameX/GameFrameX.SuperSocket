namespace GameFrameX.SuperSocket.ReliableSession;

/// <summary>
/// 定义 ReliableSession 逻辑会话关闭原因。
/// </summary>
/// <remarks>
/// Defines why a logical session is closed; these reasons are separate from a single physical transport connection close.
/// </remarks>
public enum ReliableSessionCloseReason : byte
{
    /// <summary>
    /// 未指定关闭原因。
    /// </summary>
    /// <remarks>
    /// No close reason has been specified.
    /// </remarks>
    None = 0,

    /// <summary>
    /// 客户端主动关闭逻辑会话。
    /// </summary>
    /// <remarks>
    /// The client initiated the logical session close.
    /// </remarks>
    ClientRequest = 1,

    /// <summary>
    /// 服务端主动关闭逻辑会话。
    /// </summary>
    /// <remarks>
    /// The server initiated the logical session close.
    /// </remarks>
    ServerRequest = 2,

    /// <summary>
    /// 逻辑会话超时。
    /// </summary>
    /// <remarks>
    /// The logical session timed out.
    /// </remarks>
    Timeout = 3,

    /// <summary>
    /// 逻辑会话恢复窗口已过期。
    /// </summary>
    /// <remarks>
    /// The recovery window expired before a valid resume completed.
    /// </remarks>
    Expired = 4,

    /// <summary>
    /// 逻辑会话违反协议规则。
    /// </summary>
    /// <remarks>
    /// The logical session violated protocol rules.
    /// </remarks>
    ProtocolError = 5,

    /// <summary>
    /// 逻辑会话被拒绝。
    /// </summary>
    /// <remarks>
    /// The logical session was rejected.
    /// </remarks>
    Rejected = 6
}
