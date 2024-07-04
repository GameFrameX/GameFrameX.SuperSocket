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
    class UdpConnectionListenerFactory : IConnectionListenerFactory
    {
        private readonly IConnectionFactoryBuilder _connectionFactoryBuilder;

        private readonly IUdpSessionIdentifierProvider _udpSessionIdentifierProvider;

        private readonly IAsyncSessionContainer _sessionContainer;

        public UdpConnectionListenerFactory(IConnectionFactoryBuilder connectionFactoryBuilder, IUdpSessionIdentifierProvider udpSessionIdentifierProvider, IAsyncSessionContainer sessionContainer)
        {
            _connectionFactoryBuilder = connectionFactoryBuilder;
            _udpSessionIdentifierProvider = udpSessionIdentifierProvider;
            _sessionContainer = sessionContainer;
        }

        public IConnectionListener CreateConnectionListener(ListenOptions options, ConnectionOptions connectionOptions, ILoggerFactory loggerFactory)
        {
            connectionOptions.Logger = loggerFactory.CreateLogger(nameof(IConnection));
            var connectionFactoryLogger = loggerFactory.CreateLogger(nameof(UdpConnectionFactory));

            var connectionFactory = _connectionFactoryBuilder.Build(options, connectionOptions);

            return new UdpConnectionListener(options, connectionOptions, connectionFactory, connectionFactoryLogger, _udpSessionIdentifierProvider, _sessionContainer);
        }
    }
}