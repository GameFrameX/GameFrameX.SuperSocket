using System;
using System.Buffers;

namespace GameFrameX.SuperSocket.WebSocket
{
    /// <summary>
    /// WebSocket操作码枚举
    /// </summary>
    public enum OpCode : sbyte
    {
        /// <summary>
        /// 握手操作码，值为-1
        /// </summary>
        Handshake = -1,

        /// <summary>
        /// 延续帧操作码，值为0
        /// </summary>
        Continuation = 0,

        /// <summary>
        /// 文本帧操作码，值为1
        /// </summary>
        Text = 1,

        /// <summary>
        /// 二进制帧操作码，值为2
        /// </summary>
        Binary = 2,

        /// <summary>
        /// 关闭连接操作码，值为8
        /// </summary>
        Close = 8,

        /// <summary>
        /// Ping操作码，值为9，用于心跳检测
        /// </summary>
        Ping = 9,

        /// <summary>
        /// Pong操作码，值为10，用于响应Ping
        /// </summary>
        Pong = 10
    }
}