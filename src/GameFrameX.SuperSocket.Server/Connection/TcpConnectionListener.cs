using System.Net.Sockets;
using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Primitives;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;
using Microsoft.Extensions.Logging;

namespace GameFrameX.SuperSocket.Server.Connection
{
    public class TcpConnectionListener : IConnectionListener
    {
        private Socket _listenSocket;

        private CancellationTokenSource _cancellationTokenSource;
        private TaskCompletionSource<bool> _stopTaskCompletionSource;
        public IConnectionFactory ConnectionFactory { get; }
        public ListenOptions Options { get; }
        private ILogger _logger;

        public TcpConnectionListener(ListenOptions options, IConnectionFactory connectionFactory, ILogger logger)
        {
            Options = options;
            ConnectionFactory = connectionFactory;
            _logger = logger;
        }

        public bool IsRunning { get; private set; }

        public bool Start()
        {
            var options = Options;

            try
            {
                var listenEndpoint = options.ToEndPoint();
                var listenSocket = _listenSocket = new Socket(listenEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                listenSocket.LingerState = new LingerOption(false, 0);

                if (options.NoDelay)
                    listenSocket.NoDelay = true;

                listenSocket.Bind(listenEndpoint);
                listenSocket.Listen(options.BackLog);

                IsRunning = true;

                _cancellationTokenSource = new CancellationTokenSource();

                KeepAccept(listenSocket).DoNotAwait();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"The listener[{this.ToString()}] failed to start.");
                return false;
            }
        }

        private async Task KeepAccept(Socket listenSocket)
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var client = await listenSocket.AcceptAsync().ConfigureAwait(false);
                    OnNewClientAccept(client);
                }
                catch (Exception e)
                {
                    if (e is ObjectDisposedException || e is NullReferenceException)
                        break;

                    if (e is SocketException se)
                    {
                        var errorCode = se.ErrorCode;

                        if (errorCode == 89 // System.Net.Sockets.SocketException (89): Operation canceled (MacOs)
                            || errorCode == 125 // System.Net.Sockets.SocketException (125): Operation canceled
                            || errorCode == 995  // System.Net.Sockets.SocketException (995): The I/O operation has been aborted because of either a thread exit or an application request
                            || errorCode == 10004) // System.Net.Sockets.SocketException (10004): A blocking Socket call was canceled.
                        {
                            _logger.LogDebug($"The listener[{this.ToString()}] was closed for the socket error: {errorCode}. {se.Message}");
                            break;
                        }
                    }

                    _logger.LogError(e, $"Listener[{this.ToString()}] failed to do AcceptAsync");
                    continue;
                }
            }

            _stopTaskCompletionSource?.TrySetResult(true);
        }

        public event NewConnectionAcceptHandler NewConnectionAccept;

        private async void OnNewClientAccept(Socket socket)
        {
            var handler = NewConnectionAccept;

            if (handler == null)
                return;

            IConnection connection = null;

            try
            {
#if NET6_0_OR_GREATER
                using var cts = CancellationTokenSourcePool.Shared.Rent(Options.ConnectionAcceptTimeOut);
#else
                using var cts = new CancellationTokenSource(Options.ConnectionAcceptTimeOut);
#endif
                connection = await ConnectionFactory.CreateConnection(socket, cts.Token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to create channel for {socket.RemoteEndPoint}.");
                return;
            }

            await handler.Invoke(this.Options, connection);
        }

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

        public override string ToString()
        {
            return Options?.ToString();
        }

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