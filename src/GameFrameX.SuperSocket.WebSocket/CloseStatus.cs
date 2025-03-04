using System;
using System.Buffers;

namespace GameFrameX.SuperSocket.WebSocket
{
    /// <summary>
    /// WebSocket 关闭状态类
    /// </summary>
    public class CloseStatus
    {
        /// <summary>
        /// 获取或设置关闭原因
        /// </summary>
        public CloseReason Reason { get; set; }

        /// <summary>
        /// 获取或设置关闭原因的文本描述
        /// </summary>
        public string ReasonText { get; set; }

        /// <summary>
        /// 获取或设置是否由远程端发起关闭
        /// </summary>
        public bool RemoteInitiated { get; set; }
    }
}