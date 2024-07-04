using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;
using Microsoft.Extensions.Logging;

namespace GameFrameX.SuperSocket.Server.Connection
{
    public class TcpConnectionListenerFactory : IConnectionListenerFactory
    {
        protected IConnectionFactoryBuilder ConnectionFactoryBuilder { get; }

        public TcpConnectionListenerFactory(IConnectionFactoryBuilder connectionFactoryBuilder)
        {
            ConnectionFactoryBuilder = connectionFactoryBuilder;
        }

        public virtual IConnectionListener CreateConnectionListener(ListenOptions options, ConnectionOptions connectionOptions, ILoggerFactory loggerFactory)
        {
            connectionOptions.Logger = loggerFactory.CreateLogger(nameof(IConnection));

            var connectionListenerLogger = loggerFactory.CreateLogger(nameof(TcpConnectionListener));

            return new TcpConnectionListener(
                options,
                CreateTcpConnectionFactory(options, connectionOptions),
                connectionListenerLogger);
        }

        protected virtual IConnectionFactory CreateTcpConnectionFactory(ListenOptions options, ConnectionOptions connectionOptions)
        {
            return ConnectionFactoryBuilder.Build(options, connectionOptions);
        }
    }
}