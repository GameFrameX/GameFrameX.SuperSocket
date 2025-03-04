using System;
using System.Buffers;

namespace GameFrameX.SuperSocket.WebSocket
{
    /// <summary>
    /// WebSocket关闭原因枚举
    /// </summary>
    public enum CloseReason : short
    {
        /// <summary>
        /// 正常关闭连接
        /// </summary>
        NormalClosure = 1000,

        /// <summary>
        /// 终端离开，可能因为服务端错误，也可能是客户端主动断开
        /// </summary>
        GoingAway = 1001,

        /// <summary>
        /// 由于协议错误而中断连接
        /// </summary>
        ProtocolError = 1002,

        /// <summary>
        /// 接收到的数据类型无法处理
        /// </summary>
        NotAcceptableData = 1003,

        /// <summary>
        /// 收到的数据帧过大
        /// </summary>
        TooLargeFrame = 1009,

        /// <summary>
        /// 收到的数据不是UTF-8编码
        /// </summary>
        InvalidUTF8 = 1007,

        /// <summary>
        /// 违反约定的策略
        /// </summary>
        ViolatePolicy = 1008,

        /// <summary>
        /// 扩展不匹配
        /// </summary>
        ExtensionNotMatch = 1010,

        /// <summary>
        /// 遇到意外情况导致连接断开
        /// </summary>
        UnexpectedCondition = 1011,

        /// <summary>
        /// 没有提供状态码
        /// </summary>
        NoStatusCode = 1005
    }
}