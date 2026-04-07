using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GameFrameX.SuperSocket.Client
{
    /// <summary>
    /// Represents a connector that establishes TCP socket connections.
    /// </summary>
    public class SocketConnector : ConnectorBase
    {
        /// <summary>
        /// Gets the local endpoint to bind the socket to.
        /// </summary>
        public EndPoint LocalEndPoint { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Nagle algorithm is disabled for the socket.
         /// When set to true, the socket will send data immediately without waiting to accumulate more data.
         /// This can improve performance for certain applications that require low latency, but may increase network traffic.
         /// The default value is true.
         /// </summary>
        public bool NoDelay { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketConnector"/> class with default settings.
        /// </summary>
        public SocketConnector()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketConnector"/> class with the specified local endpoint.
        /// </summary>
        /// <param name="localEndPoint">The local endpoint to bind the socket to.</param>
        public SocketConnector(EndPoint localEndPoint)
            : base()
        {
            LocalEndPoint = localEndPoint;
        }

        /// <summary>
        /// Configures the socket with the specified settings before connecting.
         /// This method can be overridden to customize the socket configuration, such as setting socket options or binding to a local endpoint.
         /// By default, it sets the NoDelay property and binds to the LocalEndPoint if it is specified.
        /// </summary>
        /// <param name="socket">The socket to configure.</param>
        protected virtual void ConfigureSocket(Socket socket)
        {
            socket.NoDelay = NoDelay;

            var localEndPoint = LocalEndPoint;

            if (localEndPoint != null)
            {
                socket.ExclusiveAddressUse = false;
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                socket.Bind(localEndPoint);
            }
        }

        /// <summary>
        /// Asynchronously connects to a remote endpoint using a TCP socket.
        /// </summary>
        /// <param name="remoteEndPoint">The remote endpoint to connect to.</param>
        /// <param name="state">The connection state object.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous connection operation.</returns>
        protected override async ValueTask<ConnectState> ConnectAsync(EndPoint remoteEndPoint, ConnectState state, CancellationToken cancellationToken)
        {
            var addressFamily = remoteEndPoint.AddressFamily;

            if (addressFamily == AddressFamily.Unspecified)
                addressFamily = AddressFamily.InterNetwork;

            var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                ConfigureSocket(socket);
                
#if NET5_0_OR_GREATER
                await socket.ConnectAsync(remoteEndPoint, cancellationToken);
#else
                Task connectTask = socket.ConnectAsync(remoteEndPoint);

                var tcs = new TaskCompletionSource<bool>();
                cancellationToken.Register(() => tcs.SetResult(false));

                await Task.WhenAny(new[] { connectTask, tcs.Task }).Unwrap();

                if (!socket.Connected)
                {
                    socket.Close();

                    return new ConnectState
                    {
                        Result = false,
                    };
                }
#endif
            }
            catch (Exception e)
            {
                return new ConnectState
                {
                    Result = false,
                    Exception = e
                };
            }

            return new ConnectState
            {
                Result = true,
                Socket = socket
            };            
        }
    }
}