using System;
using System.Buffers;

namespace GameFrameX.SuperSocket.WebSocket
{
    public enum OpCode : sbyte
    {
        Handshake = -1,
        Continuation = 0,
        Text = 1,
        Binary = 2,
        Close = 8,
        Ping = 9,
        Pong = 10
    }
}