namespace GameFrameX.SuperSocket.ReliableSession;

/// <summary>
/// 表示恢复窗口内保持稳定的 ReliableSession 逻辑会话标识。
/// </summary>
/// <remarks>
/// Identifies the logical ReliableSession session that can survive physical connection rebuilds inside the recovery window.
/// </remarks>
/// <param name="Value">逻辑会话的唯一值 / Unique value of the logical session</param>
public readonly record struct SessionId(Guid Value)
{
    /// <summary>
    /// 获取逻辑会话标识是否为空。
    /// </summary>
    /// <remarks>
    /// Gets whether the logical session identifier is empty.
    /// </remarks>
    /// <value>如果标识为空则为 <c>true</c>；否则为 <c>false</c> / <c>true</c> if the identifier is empty; otherwise <c>false</c></value>
    public bool IsEmpty => Value == Guid.Empty;
}

/// <summary>
/// 表示一次物理连接的 ReliableSession 标识。
/// </summary>
/// <remarks>
/// Identifies one physical connection attempt; it must change when TCP, KCP, UDP endpoint binding, or WebSocket connection is rebuilt.
/// </remarks>
/// <param name="Value">物理连接的唯一值 / Unique value of the physical connection</param>
public readonly record struct ConnectionId(Guid Value)
{
    /// <summary>
    /// 获取物理连接标识是否为空。
    /// </summary>
    /// <remarks>
    /// Gets whether the physical connection identifier is empty.
    /// </remarks>
    /// <value>如果标识为空则为 <c>true</c>；否则为 <c>false</c> / <c>true</c> if the identifier is empty; otherwise <c>false</c></value>
    public bool IsEmpty => Value == Guid.Empty;
}

/// <summary>
/// 表示拥有逻辑会话的客户端运行实例标识。
/// </summary>
/// <remarks>
/// Identifies the client runtime instance used to reject stale or competing resume attempts for the same logical session.
/// </remarks>
/// <param name="Value">客户端实例的唯一值 / Unique value of the client instance</param>
public readonly record struct ClientInstanceId(Guid Value)
{
    /// <summary>
    /// 获取客户端实例标识是否为空。
    /// </summary>
    /// <remarks>
    /// Gets whether the client instance identifier is empty.
    /// </remarks>
    /// <value>如果标识为空则为 <c>true</c>；否则为 <c>false</c> / <c>true</c> if the identifier is empty; otherwise <c>false</c></value>
    public bool IsEmpty => Value == Guid.Empty;
}

/// <summary>
/// 表示单个逻辑会话内的业务消息标识。
/// </summary>
/// <remarks>
/// Identifies a business message for idempotent processing and deduplication inside one logical session.
/// </remarks>
/// <param name="Value">业务消息的递增或唯一值 / Incremental or unique value of the business message</param>
public readonly record struct MessageId(ulong Value);

/// <summary>
/// 表示逻辑会话单向有序投递使用的序号。
/// </summary>
/// <remarks>
/// Identifies ordering, replay cursors, acknowledgement ranges, and gap detection for one direction of a logical session.
/// </remarks>
/// <param name="Value">序号值 / Sequence value</param>
public readonly record struct Sequence(ulong Value);

/// <summary>
/// 表示逻辑会话成功恢复后的代次。
/// </summary>
/// <remarks>
/// Identifies the resume generation; frames from older generations must be discarded by later runtime implementations.
/// </remarks>
/// <param name="Value">恢复代次值 / Resume generation value</param>
public readonly record struct ResumeGeneration(ulong Value);

/// <summary>
/// 表示业务快照版本。
/// </summary>
/// <remarks>
/// Identifies the snapshot baseline used when replay cannot cover the receiver gap.
/// </remarks>
/// <param name="Value">快照版本值，负数表示未提供 / Snapshot version value; negative means absent</param>
public readonly record struct SnapshotVersion(long Value)
{
    /// <summary>
    /// 获取快照版本是否包含有效值。
    /// </summary>
    /// <remarks>
    /// Gets whether the snapshot version contains a meaningful value.
    /// </remarks>
    /// <value>如果版本非负则为 <c>true</c>；否则为 <c>false</c> / <c>true</c> if the version is non-negative; otherwise <c>false</c></value>
    public bool HasValue => Value >= 0;
}

/// <summary>
/// 表示逻辑会话恢复使用的不透明令牌。
/// </summary>
/// <remarks>
/// Represents the opaque token used to authorize resume attempts; runtime implementations must support expiration and rotation.
/// </remarks>
/// <param name="Value">恢复令牌文本 / Resume token text</param>
public readonly record struct ResumeToken(string Value)
{
    /// <summary>
    /// 获取恢复令牌是否为空。
    /// </summary>
    /// <remarks>
    /// Gets whether the resume token is empty or whitespace.
    /// </remarks>
    /// <value>如果令牌为空白则为 <c>true</c>；否则为 <c>false</c> / <c>true</c> if the token is empty or whitespace; otherwise <c>false</c></value>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    /// <summary>
    /// 返回恢复令牌文本。
    /// </summary>
    /// <remarks>
    /// Returns the resume token text and normalizes null to an empty string.
    /// </remarks>
    /// <returns>恢复令牌文本 / Resume token text</returns>
    public override string ToString()
    {
        return Value ?? string.Empty;
    }
}

/// <summary>
/// 表示连续确认的 ReliableSession 序号范围。
/// </summary>
/// <remarks>
/// Represents a continuous acknowledgement range used to release replay-window entries after the receiver accepts data.
/// </remarks>
public readonly record struct AckRange
{
    /// <summary>
    /// 初始化 <see cref="AckRange"/> 结构的新实例。
    /// </summary>
    /// <remarks>
    /// Initializes an acknowledgement range; the start sequence must not be greater than the end sequence.
    /// </remarks>
    /// <param name="start">确认范围的起始序号 / First acknowledged sequence</param>
    /// <param name="end">确认范围的结束序号 / Last acknowledged sequence</param>
    /// <exception cref="ArgumentException">当 <paramref name="start"/> 大于 <paramref name="end"/> 时抛出 / Thrown when <paramref name="start"/> is greater than <paramref name="end"/></exception>
    public AckRange(Sequence start, Sequence end)
    {
        if (start.Value > end.Value)
            throw new ArgumentException("The start sequence must be less than or equal to the end sequence.", nameof(start));

        Start = start;
        End = end;
    }

    /// <summary>
    /// 获取确认范围的起始序号。
    /// </summary>
    /// <remarks>
    /// Gets the first acknowledged sequence.
    /// </remarks>
    /// <value>确认范围的起始序号 / First acknowledged sequence</value>
    public Sequence Start { get; }

    /// <summary>
    /// 获取确认范围的结束序号。
    /// </summary>
    /// <remarks>
    /// Gets the last acknowledged sequence.
    /// </remarks>
    /// <value>确认范围的结束序号 / Last acknowledged sequence</value>
    public Sequence End { get; }

    /// <summary>
    /// 获取确认范围包含的序号数量。
    /// </summary>
    /// <remarks>
    /// Gets the number of acknowledged sequence values in the range.
    /// </remarks>
    /// <value>确认的序号数量 / Number of acknowledged sequence values</value>
    public ulong Count => End.Value - Start.Value + 1;
}
