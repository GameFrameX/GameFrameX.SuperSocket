using System;
using System.Buffers;
using GameFrameX.SuperSocket.ProtoBase;

namespace GameFrameX.SuperSocket.WebSocket
{
    /// <summary>
    /// WebSocket数据包类，实现IWebSocketFrameHeader接口
    /// </summary>
    public class WebSocketPackage : IWebSocketFrameHeader
    {
        /// <summary>
        /// 获取或设置WebSocket操作码
        /// </summary>
        public OpCode OpCode { get; set; }

        /// <summary>
        /// 操作码字节的内部表示
        /// </summary>
        internal byte OpCodeByte { get; set; }

        /// <summary>
        /// 获取或设置FIN标志位，表示是否为消息的最后一个分片
        /// </summary>
        public bool FIN
        {
            get { return ((OpCodeByte & 0x80) == 0x80); }
            set
            {
                if (value)
                    OpCodeByte = (byte)(OpCodeByte | 0x80);
                else
                    OpCodeByte = (byte)(OpCodeByte ^ 0x80);
            }
        }

        /// <summary>
        /// 获取或设置RSV1保留位
        /// </summary>
        public bool RSV1
        {
            get { return ((OpCodeByte & 0x40) == 0x40); }
            set
            {
                if (value)
                    OpCodeByte = (byte)(OpCodeByte | 0x40);
                else
                    OpCodeByte = (byte)(OpCodeByte ^ 0x40);
            }
        }

        /// <summary>
        /// 获取或设置RSV2保留位
        /// </summary>
        public bool RSV2
        {
            get { return ((OpCodeByte & 0x20) == 0x20); }
            set
            {
                if (value)
                    OpCodeByte = (byte)(OpCodeByte | 0x20);
                else
                    OpCodeByte = (byte)(OpCodeByte ^ 0x20);
            }
        }

        /// <summary>
        /// 获取或设置RSV3保留位
        /// </summary>
        public bool RSV3
        {
            get { return ((OpCodeByte & 0x10) == 0x10); }
            set
            {
                if (value)
                    OpCodeByte = (byte)(OpCodeByte | 0x10);
                else
                    OpCodeByte = (byte)(OpCodeByte ^ 0x10);
            }
        }

        /// <summary>
        /// 保存操作码字节
        /// </summary>
        internal void SaveOpCodeByte()
        {
            OpCodeByte = (byte)((OpCodeByte & 0xF0) | (byte)OpCode);
        }

        /// <summary>
        /// 获取或设置是否使用掩码
        /// </summary>
        internal bool HasMask { get; set; }

        /// <summary>
        /// 获取或设置负载数据的长度
        /// </summary>
        internal long PayloadLength { get; set; }

        /// <summary>
        /// 获取或设置掩码密钥
        /// </summary>
        internal byte[] MaskKey { get; set; }

        /// <summary>
        /// 获取或设置WebSocket消息内容
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 获取或设置HTTP头信息
        /// </summary>
        public HttpHeader HttpHeader { get; set; }

        /// <summary>
        /// 获取或设置数据序列
        /// </summary>
        public ReadOnlySequence<byte> Data { get; set; }

        /// <summary>
        /// 序列段的头部
        /// </summary>
        internal SequenceSegment Head { get; set; }

        /// <summary>
        /// 序列段的尾部
        /// </summary>
        internal SequenceSegment Tail { get; set; }

        /// <summary>
        /// 连接两个序列
        /// </summary>
        /// <param name="second">要连接的第二个序列</param>
        internal void ConcatSequence(ref ReadOnlySequence<byte> second)
        {
            if (Head == null)
            {
                (Head, Tail) = second.DestructSequence();
                return;
            }

            if (!second.IsEmpty)
            {
                foreach (var segment in second)
                {
                    Tail = Tail.SetNext(new SequenceSegment(segment));
                }
            }
        }

        /// <summary>
        /// 构建数据序列
        /// </summary>
        internal void BuildData()
        {
            Data = new ReadOnlySequence<byte>(Head, 0, Tail, Tail.Memory.Length);
        }
    }
}