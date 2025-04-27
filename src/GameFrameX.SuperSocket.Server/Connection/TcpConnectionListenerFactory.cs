using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;
using Microsoft.Extensions.Logging;

namespace GameFrameX.SuperSocket.Server.Connection
{
    /// <summary>
    /// Factory for creating TCP connection listeners.
    /// </summary>
    public class TcpConnectionListenerFactory : IConnectionListenerFactory
    {
        /// <summary>
        /// Gets the connection factory builder used to create connection factories.
        /// </summary>
        protected IConnectionFactoryBuilder ConnectionFactoryBuilder { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpConnectionListenerFactory"/> class.
        /// </summary>
        /// <param name="connectionFactoryBuilder">The builder for creating connection factories.</param>
        public TcpConnectionListenerFactory(IConnectionFactoryBuilder connectionFactoryBuilder)
        {
            ConnectionFactoryBuilder = connectionFactoryBuilder;
        }

        /// <summary>
        /// Creates a new TCP connection listener.
        /// </summary>
        /// <param name="options">The options for the listener.</param>
        /// <param name="connectionOptions">The options for the connection.</param>
        /// <param name="loggerFactory">The factory for creating loggers.</param>
        /// <returns>A new instance of <see cref="IConnectionListener"/>.</returns>
        public virtual IConnectionListener CreateConnectionListener(ListenOptions options, ConnectionOptions connectionOptions, ILoggerFactory loggerFactory)
        {
            connectionOptions.Logger = loggerFactory.CreateLogger(nameof(IConnection));

            var connectionListenerLogger = loggerFactory.CreateLogger(nameof(TcpConnectionListener));

            return new TcpConnectionListener(
                options,
                CreateTcpConnectionFactory(options, connectionOptions),
                connectionListenerLogger);
        }

        /// <summary>
        /// Creates a TCP connection factory using the specified options.
        /// </summary>
        /// <param name="options">The options for the listener.</param>
        /// <param name="connectionOptions">The options for the connection.</param>
        /// <returns>A new instance of <see cref="IConnectionFactory"/>.</returns>
        protected virtual IConnectionFactory CreateTcpConnectionFactory(ListenOptions options, ConnectionOptions connectionOptions)
        {
            return ConnectionFactoryBuilder.Build(options, connectionOptions);
        }
    }
}