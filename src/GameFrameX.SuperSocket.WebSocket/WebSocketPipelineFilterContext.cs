using System;
using System.Buffers;
using System.Collections.Generic;
using GameFrameX.SuperSocket.WebSocket.Extensions;

namespace GameFrameX.SuperSocket.WebSocket
{
    /// <summary>
    /// WebSocket 管道过滤器上下文
    /// </summary>
    public class WebSocketPipelineFilterContext
    {
        /// <summary>
        /// 获取或设置 WebSocket 扩展列表
        /// </summary>
        public IReadOnlyList<IWebSocketExtension> Extensions { get; set; }
    }
}