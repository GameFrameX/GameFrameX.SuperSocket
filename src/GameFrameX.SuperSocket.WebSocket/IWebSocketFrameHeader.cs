using System;
using System.Buffers;
using System.Collections.Specialized;

namespace GameFrameX.SuperSocket.WebSocket
{
    public interface IWebSocketFrameHeader
    {
        bool FIN { get; }

        bool RSV1 { get; }

        bool RSV2 { get; }

        bool RSV3 { get; }
    }
}