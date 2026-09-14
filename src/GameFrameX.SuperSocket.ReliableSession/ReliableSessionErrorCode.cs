namespace GameFrameX.SuperSocket.ReliableSession;

/// <summary>
/// 定义 ReliableSession 协议错误码。
/// </summary>
/// <remarks>
/// Defines protocol-level errors that must be handled by the ReliableSession layer and must not be delivered as business packages.
/// </remarks>
public enum ReliableSessionErrorCode : byte
{
    /// <summary>
    /// 未知协议错误。
    /// </summary>
    /// <remarks>
    /// Unknown protocol error.
    /// </remarks>
    Unknown = 0,

    /// <summary>
    /// 线格式版本不受支持。
    /// </summary>
    /// <remarks>
    /// The wire version is not supported.
    /// </remarks>
    InvalidVersion = 1,

    /// <summary>
    /// 一个或多个协议字段不合法。
    /// </summary>
    /// <remarks>
    /// One or more protocol fields are invalid.
    /// </remarks>
    InvalidField = 2,

    /// <summary>
    /// 恢复令牌已过期。
    /// </summary>
    /// <remarks>
    /// The resume token is expired.
    /// </remarks>
    TokenExpired = 3,

    /// <summary>
    /// 重放窗口无法覆盖请求的游标。
    /// </summary>
    /// <remarks>
    /// The replay window cannot satisfy the requested cursor.
    /// </remarks>
    ReplayWindowExceeded = 4,

    /// <summary>
    /// 继续重放前必须先应用快照。
    /// </summary>
    /// <remarks>
    /// A snapshot is required before replay can continue.
    /// </remarks>
    SnapshotRequired = 5,

    /// <summary>
    /// 协议帧违反 ReliableSession 语义。
    /// </summary>
    /// <remarks>
    /// The frame violates ReliableSession protocol semantics.
    /// </remarks>
    ProtocolViolation = 6,

    /// <summary>
    /// 逻辑会话被拒绝。
    /// </summary>
    /// <remarks>
    /// The logical session was rejected.
    /// </remarks>
    Rejected = 7
}
