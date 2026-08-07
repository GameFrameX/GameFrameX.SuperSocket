namespace GameFrameX.SuperSocket.Kcp;

/// <summary>
/// KCP 连接配置选项。
/// </summary>
public sealed class KcpConnectionOptions
{
    /// <summary>
    /// 会话 ID（Conv）。0 表示由客户端决定。
    /// </summary>
    public uint Conv { get; set; }

    /// <summary>
    /// 最大传输单元。null 表示使用 KCP 内部默认值。
    /// </summary>
    public uint? Mtu { get; set; }

    /// <summary>
    /// 发送窗口大小。null 表示使用 KCP 内部默认值。
    /// </summary>
    public int? SendWindow { get; set; }

    /// <summary>
    /// 接收窗口大小。null 表示使用 KCP 内部默认值。
    /// </summary>
    public int? ReceiveWindow { get; set; }

    /// <summary>
    /// 是否启用 NoDelay 模式。
    /// true: 快速重传，适合实时游戏。
    /// false: 普通模式，流量利用更高。
    /// null 表示使用 KCP 内部默认值。
    /// </summary>
    public bool? NoDelay { get; set; }

    /// <summary>
    /// NoDelay 模式下的内部参数。
    /// 0: 关闭; 1: 开启快速重传; 2: 极速模式。
    /// null 表示使用 KCP 内部默认值。
    /// </summary>
    public int? NoDelayLevel { get; set; }

    /// <summary>
    /// Update 间隔（毫秒）。null 表示使用 KCP 内部默认值。
    /// </summary>
    public int? Interval { get; set; }

    /// <summary>
    /// 快速重传阈值。0 表示关闭；null 表示使用 KCP 内部默认值。
    /// </summary>
    public int? Resend { get; set; }

    /// <summary>
    /// 单个 KCP 数据段允许的最大重传次数。null 表示使用 KCP 内部默认值；需要分钟级断网恢复时应显式调高。
    /// </summary>
    public int? DeadLink { get; set; }

    /// <summary>
    /// 是否关闭拥塞控制。true=关闭；null 表示使用 KCP 内部默认值。
    /// </summary>
    public bool? NoCongestionControl { get; set; }

    /// <summary>
    /// 连接空闲超时（秒）。null 表示由连接层使用内部默认值。
    /// </summary>
    public int? IdleTimeout { get; set; }

    /// <summary>
    /// 单个 UDP datagram 的最大长度。null 或小于等于 0 表示按 MTU 或 KCP 内部默认 MTU 自动推导。
    /// </summary>
    public int? MaxDatagramSize { get; set; }

    /// <summary>
    /// 段对象池最大大小。null 表示使用段池内部默认值。
    /// </summary>
    public int? SegmentPoolSize { get; set; }

    /// <summary>
    /// 是否启用流模式。null 表示使用 KCP 内部默认值。
    /// 流模式：数据被视为连续流，不保持消息边界。
    /// 消息模式：每次 Send 视为一个独立消息，保持边界。
    /// </summary>
    public bool? StreamMode { get; set; }

    /// <summary>
    /// 快速 ACK 计数上限。0 表示不限制；null 表示使用 KCP 内部默认值。
    /// </summary>
    public int? FastAckLimit { get; set; }

    /// <summary>
    /// 初始 RTO（毫秒）。null 表示使用 KCP 内部默认值。
    /// </summary>
    public int? InitialRto { get; set; }

    /// <summary>
    /// 最小 RTO（毫秒）。null 表示使用 KCP 内部默认值。
    /// </summary>
    public int? MinRto { get; set; }

    /// <summary>
    /// 最大 RTO（毫秒）。null 表示使用 KCP 内部默认值。
    /// </summary>
    public int? MaxRto { get; set; }

    /// <summary>
    /// 窗口探测初始间隔（毫秒）。null 表示使用 KCP 内部默认值。
    /// </summary>
    public int? ProbeInit { get; set; }

    /// <summary>
    /// 窗口探测最大间隔（毫秒）。null 表示使用 KCP 内部默认值。
    /// </summary>
    public int? ProbeLimit { get; set; }

    /// <summary>
    /// 初始拥塞窗口。null 表示使用 KCP 内部默认值。
    /// </summary>
    public int? InitialCongestionWindow { get; set; }

    /// <summary>
    /// 慢启动阈值。null 表示使用 KCP 内部默认值。
    /// </summary>
    public int? SlowStartThreshold { get; set; }
}