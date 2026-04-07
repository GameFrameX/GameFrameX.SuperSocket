using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Connection;
using Microsoft.Extensions.Logging;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;
using GameFrameX.SuperSocket.Server.Abstractions.Session;

namespace GameFrameX.SuperSocket.Udp
{
    /// <summary>
    /// Creates connection listeners for UDP connections.
    /// </summary>
    class UdpConnectionListenerFactory : IConnectionListenerFactory
    {
        private readonly IConnectionFactoryBuilder _connectionFactoryBuilder;

        private readonly IUdpSessionIdentifierProvider _udpSessionIdentifierProvider;

        private readonly IAsyncSessionContainer _sessionContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpConnectionListenerFactory"/> class.
        /// </summary>
        /// <param name="connectionFactoryBuilder">The builder for creating connection factories.</param>
        /// <param name="udpSessionIdentifierProvider">The provider for UDP session identifiers.</param>
        /// <param name="sessionContainer">The container for managing sessions.</param>
        public UdpConnectionListenerFactory(IConnectionFactoryBuilder connectionFactoryBuilder, IUdpSessionIdentifierProvider udpSessionIdentifierProvider, IAsyncSessionContainer sessionContainer)
        {
            _connectionFactoryBuilder = connectionFactoryBuilder;
            _udpSessionIdentifierProvider = udpSessionIdentifierProvider;
            _sessionContainer = sessionContainer;
        }

        /// <summary>
        /// Creates a connection listener based on the specified options.
        /// </summary>
        /// <param name="options">The options for the listener.</param>
        /// <param name="connectionOptions">The options for the connection.</param>
        /// <param name="loggerFactory">The factory for creating loggers.</param>
        /// <returns>A connection listener for UDP connections.</returns>
        public IConnectionListener CreateConnectionListener(ListenOptions options, ConnectionOptions connectionOptions, ILoggerFactory loggerFactory)
        {
            connectionOptions.Logger = loggerFactory.CreateLogger(nameof(IConnection));
            var connectionFactoryLogger = loggerFactory.CreateLogger(nameof(UdpConnectionFactory));

            var connectionFactory = _connectionFactoryBuilder.Build(options, connectionOptions);

            return new UdpConnectionListener(options, connectionOptions, connectionFactory, connectionFactoryLogger, _udpSessionIdentifierProvider, _sessionContainer);
        }
    }
}