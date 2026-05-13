using System;
using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;
using GameFrameX.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameFrameX.SuperSocket.Kcp
{
    /// <summary>
    /// 创建 KCP 连接监听器的工厂。
    /// </summary>
    internal class KcpConnectionListenerFactory : IConnectionListenerFactory
    {
        private readonly IConnectionFactoryBuilder _connectionFactoryBuilder;
        private readonly IKcpSessionIdentifierProvider _identifierProvider;
        private readonly IAsyncSessionContainer _sessionContainer;
        private readonly KcpConnectionOptions _kcpOptions;

        /// <summary>
        /// 初始化 KCP 监听器工厂。
        /// </summary>
        /// <param name="connectionFactoryBuilder">连接工厂构建器</param>
        /// <param name="identifierProvider">会话标识提供者</param>
        /// <param name="sessionContainer">会话容器</param>
        /// <param name="kcpOptions">KCP 配置选项</param>
        public KcpConnectionListenerFactory(
            IConnectionFactoryBuilder connectionFactoryBuilder,
            IKcpSessionIdentifierProvider identifierProvider,
            IAsyncSessionContainer sessionContainer,
            IOptions<KcpConnectionOptions> kcpOptions)
        {
            _connectionFactoryBuilder = connectionFactoryBuilder;
            _identifierProvider = identifierProvider;
            _sessionContainer = sessionContainer;
            _kcpOptions = kcpOptions?.Value ?? new KcpConnectionOptions();
        }

        /// <summary>
        /// 创建 KCP 连接监听器。
        /// </summary>
        /// <param name="options">监听选项</param>
        /// <param name="connectionOptions">连接选项</param>
        /// <param name="loggerFactory">日志工厂</param>
        /// <returns>KCP 连接监听器</returns>
        public IConnectionListener CreateConnectionListener(
            ListenOptions options,
            ConnectionOptions connectionOptions,
            ILoggerFactory loggerFactory)
        {
            connectionOptions.Logger = loggerFactory.CreateLogger(nameof(IConnection));
            var listenerLogger = loggerFactory.CreateLogger(nameof(KcpConnectionListener));

            var connectionFactory = _connectionFactoryBuilder.Build(options, connectionOptions);

            return new KcpConnectionListener(
                options,
                connectionOptions,
                connectionFactory,
                _kcpOptions,
                listenerLogger,
                _identifierProvider,
                _sessionContainer);
        }
    }
}
