using System;
using System.Net;
using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;

namespace GameFrameX.SuperSocket.Udp
{
    public class UdpConnectionFactoryBuilder : IConnectionFactoryBuilder
    {
        public IConnectionFactory Build(ListenOptions listenOptions, ConnectionOptions connectionOptions)
        {
            return new UdpConnectionFactory();
        }
    }
}