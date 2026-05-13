using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Primitives;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;
using GameFrameX.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameFrameX.SuperSocket.Kcp
{
    /// <summary>
    /// KCP 连接监听器。
    /// 在 UDP Socket 上监听，按 Conv 标识路由到不同的 KcpPipeConnection。
    /// </summary>
    internal class KcpConnectionListener : IConnectionListener
    {
        private readonly ILogger _logger;
        private Socket _listenSocket;
        private IPEndPoint _acceptRemoteEndPoint;
        private readonly IKcpSessionIdentifierProvider _identifierProvider;
        private readonly IAsyncSessionContainer _sessionContainer;
        private readonly KcpConnectionOptions _kcpOptions;
        private CancellationTokenSource _cancellationTokenSource;
        private TaskCompletionSource<bool> _stopTaskCompletionSource;
        private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        /// <summary>
        /// 获取连接工厂。
        /// </summary>
        public IConnectionFactory ConnectionFactory { get; }

        /// <summary>
        /// 获取监听选项。
        /// </summary>
        public ListenOptions Options { get; }

        /// <summary>
        /// 获取连接选项。
        /// </summary>
        public ConnectionOptions ConnectionOptions { get; }

        /// <summary>
        /// 获取监听器是否正在运行。
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 新连接接受事件。
        /// </summary>
        public event NewConnectionAcceptHandler NewConnectionAccept;

        /// <summary>
        /// 初始化 KCP 连接监听器。
        /// </summary>
        /// <param name="options">监听选项</param>
        /// <param name="connectionOptions">连接选项</param>
        /// <param name="connectionFactory">连接工厂</param>
        /// <param name="kcpOptions">KCP 配置选项</param>
        /// <param name="logger">日志实例</param>
        /// <param name="identifierProvider">会话标识提供者</param>
        /// <param name="sessionContainer">会话容器</param>
        public KcpConnectionListener(
            ListenOptions options,
            ConnectionOptions connectionOptions,
            IConnectionFactory connectionFactory,
            KcpConnectionOptions kcpOptions,
            ILogger logger,
            IKcpSessionIdentifierProvider identifierProvider,
            IAsyncSessionContainer sessionContainer)
        {
            Options = options;
            ConnectionOptions = connectionOptions;
            ConnectionFactory = connectionFactory;
            _kcpOptions = kcpOptions;
            _logger = logger;
            _identifierProvider = identifierProvider;
            _sessionContainer = sessionContainer;
        }

        /// <summary>
        /// 启动 KCP 监听器。
        /// </summary>
        /// <returns>启动成功返回 true</returns>
        public bool Start()
        {
            var options = Options;

            try
            {
                var listenEndpoint = options.ToEndPoint();
                var listenSocket = _listenSocket = new Socket(listenEndpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                if (options.NoDelay)
                    listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);

                listenSocket.ExclusiveAddressUse = options.UdpExclusiveAddressUse;
                listenSocket.Bind(listenEndpoint);

                _acceptRemoteEndPoint = listenEndpoint.AddressFamily == AddressFamily.InterNetworkV6
                    ? new IPEndPoint(IPAddress.IPv6Any, 0)
                    : new IPEndPoint(IPAddress.Any, 0);

                // 禁用 ICMP Port Unreachable 导致的异常（Windows 平台）
                uint IOC_IN = 0x80000000;
                uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

                byte[] optionInValue = { Convert.ToByte(false) };
                byte[] optionOutValue = new byte[4];

                try
                {
                    listenSocket.IOControl((int)SIO_UDP_CONNRESET, optionInValue, optionOutValue);
                }
                catch (PlatformNotSupportedException)
                {
                    _logger.LogWarning("Failed to set socket option SIO_UDP_CONNRESET because the platform doesn't support it.");
                }

                IsRunning = true;
                _cancellationTokenSource = new CancellationTokenSource();

                KeepAccept(listenSocket).DoNotAwait();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"The listener[{this}] failed to start.");
                return false;
            }
        }

        /// <summary>
        /// 核心接收循环。
        /// </summary>
        private async Task KeepAccept(Socket listenSocket)
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                byte[] buffer = null;

                try
                {
                    var bufferSize = ConnectionOptions.MaxPackageLength;
                    buffer = _bufferPool.Rent(bufferSize);

                    var result = await listenSocket
                        .ReceiveFromAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), SocketFlags.None, _acceptRemoteEndPoint)
                        .ConfigureAwait(false);

                    var receivedBytes = result.ReceivedBytes;
                    var remoteEndPoint = result.RemoteEndPoint as IPEndPoint;

                    if (receivedBytes < Kcp.KcpConstants.IKCP_OVERHEAD)
                    {
                        // 数据太短，不是有效的 KCP 包
                        continue;
                    }

                    // 从 UDP 包提取会话标识
                    string sessionID;
                    try
                    {
                        sessionID = _identifierProvider.GetSessionIdentifier(
                            remoteEndPoint, new ReadOnlySpan<byte>(buffer, 0, receivedBytes));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get session identifier from KCP packet");
                        continue;
                    }

                    // 查找已有 Session
                    var session = await _sessionContainer.GetSessionByIDAsync(sessionID);

                    if (session != null)
                    {
                        // 已有连接 → 直接投递 UDP 数据给 KCP
                        var kcpConn = session.Connection as KcpPipeConnection;
                        if (kcpConn != null)
                        {
                            kcpConn.InputUdpPacket(new ReadOnlyMemory<byte>(buffer, 0, receivedBytes));
                        }
                    }
                    else
                    {
                        // 新连接 → 创建 KcpPipeConnection
                        var connection = await CreateConnection(listenSocket, remoteEndPoint, sessionID);

                        if (connection == null)
                            continue;

                        OnNewConnectionAccept(connection);

                        // 首包也要投递给 KCP
                        var kcpConn = connection as KcpPipeConnection;
                        kcpConn?.InputUdpPacket(new ReadOnlyMemory<byte>(buffer, 0, receivedBytes));
                    }
                }
                catch (Exception e)
                {
                    if (e is ObjectDisposedException || e is NullReferenceException)
                        break;

                    if (e is SocketException se)
                    {
                        var errorCode = se.ErrorCode;

                        // 监听 Socket 已关闭
                        if (errorCode == 125 || errorCode == 89 || errorCode == 995 || errorCode == 10004 || errorCode == 10038)
                        {
                            break;
                        }
                    }

                    _logger.LogError(e, $"Listener[{this}] failed to receive KCP data");
                }
                finally
                {
                    if (buffer != null)
                        _bufferPool.Return(buffer);
                }
            }

            _stopTaskCompletionSource?.TrySetResult(true);
        }

        private void OnNewConnectionAccept(IConnection connection)
        {
            var handler = NewConnectionAccept;
            handler?.Invoke(Options, connection);
        }

        private async ValueTask<IConnection> CreateConnection(Socket socket, IPEndPoint remoteEndPoint, string sessionIdentifier)
        {
            try
            {
#if NET6_0_OR_GREATER
                using var cts = CancellationTokenSourcePool.Shared.Rent(Options.ConnectionAcceptTimeOut);
#else
                using var cts = new CancellationTokenSource(Options.ConnectionAcceptTimeOut);
#endif
                return await ConnectionFactory.CreateConnection(new KcpConnectionInfo
                {
                    Socket = socket,
                    SessionIdentifier = sessionIdentifier,
                    RemoteEndPoint = remoteEndPoint,
                    ConnectionOptions = ConnectionOptions,
                    KcpOptions = _kcpOptions
                }, cts.Token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to create KCP connection for {remoteEndPoint}.");
                return null;
            }
        }

        /// <summary>
        /// 使用指定 Socket 创建连接。
        /// </summary>
        /// <param name="connection">Socket 对象</param>
        /// <returns>创建的连接</returns>
        public async Task<IConnection> CreateConnection(object connection)
        {
            var socket = (Socket)connection;
            var remoteEndPoint = socket.RemoteEndPoint as IPEndPoint;
            return await CreateConnection(socket, remoteEndPoint, _identifierProvider.GetSessionIdentifier(remoteEndPoint, null));
        }

        /// <summary>
        /// 异步停止监听器。
        /// </summary>
        /// <returns>停止任务</returns>
        public Task StopAsync()
        {
            var listenSocket = _listenSocket;

            if (listenSocket == null)
                return Task.CompletedTask;

            _stopTaskCompletionSource = new TaskCompletionSource<bool>();

            _cancellationTokenSource.Cancel();
            listenSocket.Close();

            return _stopTaskCompletionSource.Task;
        }

        /// <summary>
        /// 返回监听器的字符串表示。
        /// </summary>
        public override string ToString()
        {
            return Options?.ToString();
        }

        /// <summary>
        /// 释放监听器资源。
        /// </summary>
        public void Dispose()
        {
            var listenSocket = _listenSocket;

            if (listenSocket != null && Interlocked.CompareExchange(ref _listenSocket, null, listenSocket) == listenSocket)
            {
                listenSocket.Dispose();
            }
        }
    }
}
