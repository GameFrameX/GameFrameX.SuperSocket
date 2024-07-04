using System;
using System.Net;
using System.Net.Sockets;
using GameFrameX.SuperSocket.Connection;

namespace GameFrameX.SuperSocket.Udp
{
    internal struct UdpConnectionInfo
    {
        public Socket Socket { get; set; }

        public ConnectionOptions ConnectionOptions { get; set; }

        public string SessionIdentifier { get; set; }

        public IPEndPoint RemoteEndPoint { get; set; }
    }
}