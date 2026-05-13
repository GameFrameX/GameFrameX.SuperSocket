namespace GameFrameX.SuperSocket.Kcp
{
    /// <summary>
    /// KCP 连接配置选项。
    /// </summary>
    public class KcpConnectionOptions
    {
        /// <summary>
        /// 会话 ID（Conv）。0 表示由客户端决定。
        /// </summary>
        public uint Conv { get; set; }

        /// <summary>
        /// 最大传输单元。默认 1400（避免 IP 分片）。
        /// </summary>
        public uint Mtu { get; set; } = 1400;

        /// <summary>
        /// 发送窗口大小。默认 256。
        /// </summary>
        public int SendWindow { get; set; } = 256;

        /// <summary>
        /// 接收窗口大小。默认 256。
        /// </summary>
        public int ReceiveWindow { get; set; } = 256;

        /// <summary>
        /// 是否启用 NoDelay 模式。
        /// true: 快速重传，适合实时游戏。
        /// false: 普通模式，流量利用更高。
        /// 默认 true。
        /// </summary>
        public bool NoDelay { get; set; } = true;

        /// <summary>
        /// NoDelay 模式下的内部参数。
        /// 0: 关闭; 1: 开启快速重传; 2: 极速模式。
        /// 默认 1。
        /// </summary>
        public int NoDelayLevel { get; set; } = 1;

        /// <summary>
        /// Update 间隔（毫秒）。默认 10。
        /// </summary>
        public int Interval { get; set; } = 10;

        /// <summary>
        /// 快速重传阈值。0 表示关闭。默认 0。
        /// </summary>
        public int Resend { get; set; } = 0;

        /// <summary>
        /// 是否关闭拥塞控制。true=关闭。默认 true。
        /// </summary>
        public bool NoCongestionControl { get; set; } = true;

        /// <summary>
        /// 连接空闲超时（秒）。默认 120。
        /// </summary>
        public int IdleTimeout { get; set; } = 120;

        /// <summary>
        /// 段对象池最大大小。默认 1024。
        /// </summary>
        public int SegmentPoolSize { get; set; } = 1024;

        /// <summary>
        /// 是否启用流模式。默认 false（消息模式）。
        /// 流模式：数据被视为连续流，不保持消息边界。
        /// 消息模式：每次 Send 视为一个独立消息，保持边界。
        /// </summary>
        public bool StreamMode { get; set; } = false;
    }
}
