using System;
using System.Net;
using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;

namespace GameFrameX.SuperSocket.Udp
{
    /// <summary>
    /// Builds a connection factory for UDP connections.
    /// </summary>
    public class UdpConnectionFactoryBuilder : IConnectionFactoryBuilder
    {
        /// <summary>
        /// Builds a connection factory based on the specified listen and connection options.
        /// </summary>
        /// <param name="listenOptions">The options for the listener.</param>
        /// <param name="connectionOptions">The options for the connection.</param>
        /// <returns>A connection factory for UDP connections.</returns>
        public IConnectionFactory Build(ListenOptions listenOptions, ConnectionOptions connectionOptions)
        {
            return new UdpConnectionFactory();
        }
    }
}