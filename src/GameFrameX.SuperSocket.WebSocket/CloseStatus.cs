using System;
using System.Buffers;

namespace GameFrameX.SuperSocket.WebSocket
{
    public class CloseStatus
    {
        public CloseReason Reason { get; set; }

        public string ReasonText { get; set; }

        public bool RemoteInitiated { get; set; }
    }
}