using System;
using System.Buffers;
using System.Collections.Generic;
using GameFrameX.SuperSocket.WebSocket.Extensions;

namespace GameFrameX.SuperSocket.WebSocket
{
    public class WebSocketPipelineFilterContext
    {
        public IReadOnlyList<IWebSocketExtension> Extensions { get; set; }
    }
}