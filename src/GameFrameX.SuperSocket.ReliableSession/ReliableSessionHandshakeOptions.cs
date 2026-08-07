namespace GameFrameX.SuperSocket.ReliableSession;

/// <summary>
/// 表示 ReliableSession 握手阶段协商的可靠会话配置。
/// </summary>
/// <remarks>
/// Represents the negotiated ReliableSession options for heartbeat, recovery window, replay, deduplication, token lifetime, and snapshot fallback.
/// </remarks>
public sealed record ReliableSessionHandshakeOptions
{
    /// <summary>
    /// 获取或设置发送 ReliableSession 心跳的间隔。
    /// </summary>
    /// <remarks>
    /// Gets or sets the interval used by the logical session heartbeat.
    /// </remarks>
    /// <value>心跳发送间隔 / Heartbeat send interval</value>
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 获取或设置等待心跳响应的超时时间。
    /// </summary>
    /// <remarks>
    /// Gets or sets the timeout used to judge heartbeat liveness.
    /// </remarks>
    /// <value>心跳超时时间 / Heartbeat timeout</value>
    public TimeSpan HeartbeatTimeout { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// 获取或设置进入可疑连接状态前允许丢失的心跳次数。
    /// </summary>
    /// <remarks>
    /// Gets or sets the number of missed heartbeats before the logical session should treat the physical connection as suspect.
    /// </remarks>
    /// <value>允许丢失的心跳次数 / Allowed missed heartbeat count</value>
    public int HeartbeatMissThreshold { get; init; } = 3;

    /// <summary>
    /// 获取或设置逻辑会话进入断线状态前的宽限时间。
    /// </summary>
    /// <remarks>
    /// Gets or sets the grace period before the logical session is considered disconnected.
    /// </remarks>
    /// <value>断线宽限时间 / Disconnect grace period</value>
    public TimeSpan DisconnectGracePeriod { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 获取或设置允许携带恢复令牌重新连接的恢复窗口。
    /// </summary>
    /// <remarks>
    /// Gets or sets how long the logical session can be resumed with a valid resume token after the physical connection is lost.
    /// </remarks>
    /// <value>会话恢复窗口 / Session recovery window</value>
    public TimeSpan RecoveryWindow { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// 获取或设置未确认业务帧的重放窗口数量上限。
    /// </summary>
    /// <remarks>
    /// Gets or sets the maximum number of unacknowledged frames retained for replay.
    /// </remarks>
    /// <value>重放窗口帧数上限 / Replay window frame limit</value>
    public int ReplayWindowSize { get; init; } = 1024;

    /// <summary>
    /// 获取或设置未确认业务帧的重放窗口字节上限。
    /// </summary>
    /// <remarks>
    /// Gets or sets the maximum bytes retained for replay-window data.
    /// </remarks>
    /// <value>重放窗口字节上限 / Replay window byte limit</value>
    public long ReplayWindowBytes { get; init; } = 4 * 1024 * 1024;

    /// <summary>
    /// 获取或设置已处理消息标识的去重窗口数量上限。
    /// </summary>
    /// <remarks>
    /// Gets or sets how many processed message identifiers can be retained for duplicate suppression.
    /// </remarks>
    /// <value>去重窗口数量上限 / Deduplication window size</value>
    public int DedupWindowSize { get; init; } = 1024;

    /// <summary>
    /// 获取或设置单次恢复允许重放的最大帧数。
    /// </summary>
    /// <remarks>
    /// Gets or sets the maximum number of frames that can be replayed during one resume flow.
    /// </remarks>
    /// <value>最大重放帧数 / Maximum replay frame count</value>
    public int MaxReplayFrames { get; init; } = 1024;

    /// <summary>
    /// 获取或设置允许缓存的乱序帧数量上限。
    /// </summary>
    /// <remarks>
    /// Gets or sets the maximum number of out-of-order frames buffered while waiting for missing sequences.
    /// </remarks>
    /// <value>乱序帧缓存数量上限 / Out-of-order frame buffer limit</value>
    public int MaxBufferedOutOfOrderFrames { get; init; } = 256;

    /// <summary>
    /// 获取或设置恢复令牌的有效期。
    /// </summary>
    /// <remarks>
    /// Gets or sets how long a resume token remains valid.
    /// </remarks>
    /// <value>恢复令牌有效期 / Resume token lifetime</value>
    public TimeSpan ResumeTokenLifetime { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// 获取或设置成功恢复后是否轮换恢复令牌。
    /// </summary>
    /// <remarks>
    /// Gets or sets whether a successful resume should rotate the resume token.
    /// </remarks>
    /// <value>如果恢复后需要轮换令牌则为 <c>true</c>；否则为 <c>false</c> / <c>true</c> if the token should be rotated after resume; otherwise <c>false</c></value>
    public bool RotateResumeToken { get; init; } = true;

    /// <summary>
    /// 获取或设置断线多久后必须使用快照兜底恢复。
    /// </summary>
    /// <remarks>
    /// Gets or sets the duration after which snapshot fallback is required before replay can continue.
    /// </remarks>
    /// <value>要求快照兜底的时间阈值 / Duration after which snapshot fallback is required</value>
    public TimeSpan SnapshotRequiredAfter { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 验证所有配置值是否位于协议允许范围内。
    /// </summary>
    /// <remarks>
    /// Validates that all configuration values are within legal protocol bounds.
    /// </remarks>
    internal void Validate()
    {
        EnsureNonNegative(HeartbeatInterval, nameof(HeartbeatInterval));
        EnsureNonNegative(HeartbeatTimeout, nameof(HeartbeatTimeout));
        EnsurePositive(HeartbeatMissThreshold, nameof(HeartbeatMissThreshold));
        EnsureNonNegative(DisconnectGracePeriod, nameof(DisconnectGracePeriod));
        EnsureNonNegative(RecoveryWindow, nameof(RecoveryWindow));
        EnsurePositive(ReplayWindowSize, nameof(ReplayWindowSize));
        EnsurePositive(ReplayWindowBytes, nameof(ReplayWindowBytes));
        EnsurePositive(DedupWindowSize, nameof(DedupWindowSize));
        EnsurePositive(MaxReplayFrames, nameof(MaxReplayFrames));
        EnsurePositive(MaxBufferedOutOfOrderFrames, nameof(MaxBufferedOutOfOrderFrames));
        EnsureNonNegative(ResumeTokenLifetime, nameof(ResumeTokenLifetime));
        EnsureNonNegative(SnapshotRequiredAfter, nameof(SnapshotRequiredAfter));
    }

    private static void EnsureNonNegative(TimeSpan value, string name)
    {
        if (value < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(name, "The value must not be negative.");
    }

    private static void EnsurePositive(int value, string name)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(name, "The value must be greater than zero.");
    }

    private static void EnsurePositive(long value, string name)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(name, "The value must be greater than zero.");
    }
}
