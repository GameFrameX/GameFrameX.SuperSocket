namespace GameFrameX.SuperSocket.Kcp.Kcp
{
    /// <summary>
    /// KCP 协议常量定义，对应 ikcp.h 中的宏定义。
    /// </summary>
    internal static class KcpConstants
    {
        /// <summary>KCP 连接请求命令</summary>
        public const byte IKCP_CMD_PUSH = 81;

        /// <summary>KCP 确认命令</summary>
        public const byte IKCP_CMD_ACK = 82;

        /// <summary>KCP 窗口探测请求命令</summary>
        public const byte IKCP_CMD_WASK = 83;

        /// <summary>KCP 窗口探测响应命令</summary>
        public const byte IKCP_CMD_WINS = 84;

        /// <summary>Ask 确认是否需要发送窗口探测</summary>
        public const int IKCP_ASK_SEND = 1;

        /// <summary>判断是否在窗口内</summary>
        public const int IKCP_INFINITE = int.MaxValue;

        /// <summary>KCP 头部大小（字节）</summary>
        public const int IKCP_OVERHEAD = 24;

        /// <summary>KCP ACK 节点偏移中的时间戳字段偏移</summary>
        public const int IKCP_ACK_OFFSET = 12;

        /// <summary>默认 MTU</summary>
        public const int IKCP_MTU_DEF = 1400;

        /// <summary>默认最大传输单元 (MTU) 对应的最大段大小</summary>
        public const int IKCP_MSS_DEF = IKCP_MTU_DEF - IKCP_OVERHEAD;

        /// <summary>默认发送窗口大小</summary>
        public const int IKCP_WND_SND = 32;

        /// <summary>默认接收窗口大小</summary>
        public const int IKCP_WND_RCV = 128;

        /// <summary>发送队列最大长度</summary>
        public const int IKCP_WND_MAX = 256;

        /// <summary>默认超时等待时间（毫秒）</summary>
        public const int IKCP_INTERVAL = 100;

        /// <summary>默认超时重传时间（毫秒）</summary>
        public const int IKCP_TIMEOUT = 60000;

        /// <summary>最大重传次数</summary>
        public const int IKCP_DEADLINK = 20;

        /// <summary>最大重传阈值</summary>
        public const int IKCP_THRESH_INIT = 2;

        /// <summary>最小重传阈值</summary>
        public const int IKCP_THRESH_MIN = 2;

        /// <summary>最小 RTO（毫秒）</summary>
        public const int IKCP_RTO_MIN = 100;

        /// <summary>最大 RTO（毫秒）</summary>
        public const int IKCP_RTO_MAX = 60000;

        /// <summary>RTO 初始值（毫秒）</summary>
        public const int IKCP_RTO_DEF = 200;

        /// <summary>RTO 增量基数</summary>
        public const int IKCP_RTO_NDL = 30;

        /// <summary>RTO 增量倍率（与 RTT 方差相关）</summary>
        public const int IKCP_RTO_SCALE = 7;

        /// <summary>快速 ACK 最大保留次数</summary>
        public const int IKCP_FASTACK_LIMIT = 5;

        /// <summary>拥塞窗口初始值</summary>
        public const int IKCP_CWND_INIT = 1;

        /// <summary>拥塞窗口增量</summary>
        public const int IKCP_CWND_INCR = 1;

        /// <summary>拥塞窗口缩减因子</summary>
        public const int IKCP_CWND_SCALE = 2;

        /// <summary>探测间隔（毫秒）</summary>
        public const int IKCP_PROBE_INIT = 7000;

        /// <summary>探测间隔上限（毫秒）</summary>
        public const int IKCP_PROBE_LIMIT = 120000;

        /// <summary>掩码：是否需要发送窗口探测</summary>
        public const int IKCP_PROBE_ASK = 1;

        /// <summary>掩码：已经发送过窗口探测</summary>
        public const int IKCP_PROBE_WAIT = 2;

        /// <summary>最小 MTU 值</summary>
        public const int IKCP_MTU_MIN = 50;

        /// <summary>日志级别：无</summary>
        public const int IKCP_LOG_NONE = 0;

        /// <summary>日志级别：数据</summary>
        public const int IKCP_LOG_DATA = 1;

        /// <summary>日志级别：错误</summary>
        public const int IKCP_LOG_ERROR = 2;

        /// <summary>日志级别：输入</summary>
        public const int IKCP_LOG_INPUT = 4;

        /// <summary>日志级别：输出</summary>
        public const int IKCP_LOG_OUTPUT = 8;

        /// <summary>日志级别：发送</summary>
        public const int IKCP_LOG_SEND = 16;

        /// <summary>日志级别：接收</summary>
        public const int IKCP_LOG_RECV = 32;

        /// <summary>日志级别：全部</summary>
        public const int IKCP_LOG_ALL = 0x3F;

        /// <summary>连接状态：可用</summary>
        public const int IKCP_STATE_AVAILABLE = 0;

        /// <summary>连接状态：已关闭</summary>
        public const int IKCP_STATE_DEAD = -1;
    }
}
