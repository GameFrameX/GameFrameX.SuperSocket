namespace GameFrameX.SuperSocket.ReliableSession;

/// <summary>
/// 定义 ReliableSession 协议帧类型。
/// </summary>
/// <remarks>
/// Defines the frame kinds currently fixed by the C3 protocol model.
/// </remarks>
public enum ReliableSessionFrameKind : byte
{
    /// <summary>
    /// 新建逻辑会话的握手请求。
    /// </summary>
    /// <remarks>
    /// Initial logical session handshake request.
    /// </remarks>
    Hello = 1,

    /// <summary>
    /// 新建逻辑会话的握手响应。
    /// </summary>
    /// <remarks>
    /// Initial logical session handshake response.
    /// </remarks>
    HelloAck = 2,

    /// <summary>
    /// 在新物理连接上恢复已有逻辑会话的请求。
    /// </summary>
    /// <remarks>
    /// Resume attempt for an existing logical session.
    /// </remarks>
    Resume = 3,

    /// <summary>
    /// 已有逻辑会话恢复后的确认响应。
    /// </summary>
    /// <remarks>
    /// Resume acknowledgement.
    /// </remarks>
    ResumeAck = 4,

    /// <summary>
    /// 逻辑会话层心跳探测。
    /// </summary>
    /// <remarks>
    /// Logical-session liveness probe.
    /// </remarks>
    Heartbeat = 5,

    /// <summary>
    /// 承载业务负载的有序数据帧。
    /// </summary>
    /// <remarks>
    /// Ordered business data frame.
    /// </remarks>
    Data = 6,

    /// <summary>
    /// 清理重放窗口使用的确认帧。
    /// </summary>
    /// <remarks>
    /// Acknowledgement frame used to release replay-window entries.
    /// </remarks>
    Ack = 7,

    /// <summary>
    /// 请求业务快照兜底恢复的帧。
    /// </summary>
    /// <remarks>
    /// Request for a business snapshot fallback.
    /// </remarks>
    SnapshotRequest = 8,

    /// <summary>
    /// 承载业务快照内容的帧。
    /// </summary>
    /// <remarks>
    /// Snapshot payload frame.
    /// </remarks>
    Snapshot = 9,

    /// <summary>
    /// 关闭逻辑会话的通知帧。
    /// </summary>
    /// <remarks>
    /// Logical session close notification.
    /// </remarks>
    Close = 10,

    /// <summary>
    /// 协议错误帧。
    /// </summary>
    /// <remarks>
    /// Protocol error frame.
    /// </remarks>
    Error = 11
}
