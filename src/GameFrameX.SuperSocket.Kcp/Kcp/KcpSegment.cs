namespace GameFrameX.SuperSocket.Kcp.Kcp
{
    /// <summary>
    /// KCP 数据段，对应 ikcp_segment。
    /// 表示 KCP 协议中的一个传输单元，包含头部信息和负载数据。
    /// </summary>
    internal class KcpSegment
    {
        /// <summary>会话 ID</summary>
        public uint Conv;

        /// <summary>命令类型（IKCP_CMD_PUSH / ACK / WASK / WINS）</summary>
        public byte Cmd;

        /// <summary>分片编号（从大到小，0 表示最后一个分片）</summary>
        public byte Frg;

        /// <summary>发送方可用窗口大小</summary>
        public ushort Wnd;

        /// <summary>时间戳（毫秒）</summary>
        public uint Ts;

        /// <summary>序列号</summary>
        public uint Sn;

        /// <summary>发送端未确认的序列号</summary>
        public uint Una;

        /// <summary>数据长度</summary>
        public uint Len;

        /// <summary>重发时间戳（毫秒）</summary>
        public uint Resendts;

        /// <summary>重传超时（毫秒）</summary>
        public uint Rto;

        /// <summary>快速确认计数</summary>
        public uint Fastack;

        /// <summary>已发送次数</summary>
        public uint Xmit;

        /// <summary>数据缓冲区</summary>
        public byte[] Data;

        /// <summary>
        /// 初始化数据段并分配指定大小的缓冲区。
        /// </summary>
        /// <param name="size">数据缓冲区大小</param>
        public KcpSegment(int size)
        {
            Data = new byte[size];
        }

        /// <summary>
        /// 重置数据段状态以便复用。
        /// 保留 Data 缓冲区，仅清除元数据。
        /// </summary>
        /// <param name="dataSize">需要的数据大小（如果当前缓冲区不足会重新分配）</param>
        public void Reset(int dataSize)
        {
            Conv = 0;
            Cmd = 0;
            Frg = 0;
            Wnd = 0;
            Ts = 0;
            Sn = 0;
            Una = 0;
            Len = 0;
            Resendts = 0;
            Rto = 0;
            Fastack = 0;
            Xmit = 0;

            if (Data == null || Data.Length < dataSize)
            {
                Data = new byte[dataSize];
            }
        }
    }
}
