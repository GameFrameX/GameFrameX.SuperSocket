using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Client;
using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.ProtoBase;
using GameFrameX.SuperSocket.Primitives;
using Xunit;

namespace GameFrameX.SuperSocket.Kcp.Tests
{
    /// <summary>
    /// KCP EasyClient 扩展测试。
    /// </summary>
    public class KcpEasyClientExtensionsTests
    {
        /// <summary>
        /// 验证 AsKcp 会创建携带指定 Conv 的 KcpPipeConnection。
        /// </summary>
        [Fact]
        public void AsKcp_Should_Create_KcpPipeConnection_With_Conv()
        {
            var client = new TestEasyClient();
            var options = new KcpConnectionOptions
            {
                Conv = 0x13572468,
                MaxDatagramSize = 1024
            };

            client.AsKcp(new IPEndPoint(IPAddress.Loopback, 26000), options, bufferSize: 0);

            var connection = Assert.IsType<KcpPipeConnection>(client.CurrentConnection);
            Assert.Equal(0x13572468U, connection.Conv);
            Assert.NotNull(client.LocalEndPoint);

            client.Dispose();
        }

        /// <summary>
        /// 验证本机 KCP 连接能复用现有 LinePipelineFilter 接收业务包。
        /// </summary>
        [Fact]
        public async Task KcpPipeConnection_Should_Deliver_Payload_To_PipelineFilter()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var clientSocket = CreateBoundSocket();
            using var serverSocket = CreateBoundSocket();

            var clientEndPoint = (IPEndPoint)clientSocket.LocalEndPoint;
            var serverEndPoint = (IPEndPoint)serverSocket.LocalEndPoint;
            var kcpOptions = new KcpConnectionOptions
            {
                Conv = 0x24681357,
                Interval = 1,
                MaxDatagramSize = 4096
            };

            var clientConnection = new KcpPipeConnection(
                clientSocket,
                serverEndPoint,
                $"client:{kcpOptions.Conv}",
                kcpOptions.Conv,
                new ConnectionOptions(),
                kcpOptions);

            var serverConnection = new KcpPipeConnection(
                serverSocket,
                clientEndPoint,
                $"server:{kcpOptions.Conv}",
                kcpOptions.Conv,
                new ConnectionOptions(),
                kcpOptions);

            clientConnection.StartUpdateLoop();
            serverConnection.StartUpdateLoop();

            var clientPump = PumpSocketAsync(clientSocket, serverEndPoint, clientConnection, cts.Token);
            var serverPump = PumpSocketAsync(serverSocket, clientEndPoint, serverConnection, cts.Token);
            var packageEnumerator = serverConnection.RunAsync(new LinePipelineFilter()).GetAsyncEnumerator(cts.Token);
            var receiveTask = packageEnumerator.MoveNextAsync().AsTask();

            await clientConnection.SendAsync(Encoding.UTF8.GetBytes("ping\r\n"), cts.Token);

            var completed = await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(3), cts.Token));
            Assert.Same(receiveTask, completed);
            Assert.True(await receiveTask);
            Assert.Equal("ping", packageEnumerator.Current.Text);

            await packageEnumerator.DisposeAsync();
            await clientConnection.CloseAsync(CloseReason.LocalClosing);
            await serverConnection.CloseAsync(CloseReason.LocalClosing);
            cts.Cancel();

            await Task.WhenAll(clientPump, serverPump);
        }

        /// <summary>
        /// 验证包编码发送不会在 KCP 发送锁上自锁。
        /// </summary>
        [Fact]
        public async Task SendPackage_Should_Not_Deadlock()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            using var socket = CreateBoundSocket();
            var remoteEndPoint = new IPEndPoint(IPAddress.Loopback, 26001);
            var kcpOptions = new KcpConnectionOptions
            {
                Conv = 0x10203040,
                MaxDatagramSize = 4096
            };

            var connection = new KcpPipeConnection(
                socket,
                remoteEndPoint,
                $"client:{kcpOptions.Conv}",
                kcpOptions.Conv,
                new ConnectionOptions(),
                kcpOptions);

            var sendTask = connection.SendAsync(new DefaultStringEncoder(), "ping", cts.Token).AsTask();
            var completed = await Task.WhenAny(sendTask, Task.Delay(TimeSpan.FromSeconds(1), cts.Token));

            Assert.Same(sendTask, completed);
            await sendTask;
            await connection.CloseAsync(CloseReason.LocalClosing);
        }

        private static Socket CreateBoundSocket()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            return socket;
        }

        private static async Task PumpSocketAsync(
            Socket socket,
            IPEndPoint expectedRemoteEndPoint,
            KcpPipeConnection connection,
            CancellationToken cancellationToken)
        {
            var receiveEndPoint = new IPEndPoint(IPAddress.Any, 0);
            var buffer = new byte[4096];

            while (!connection.IsClosed && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await socket
                        .ReceiveFromAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), SocketFlags.None, receiveEndPoint)
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (result.RemoteEndPoint is IPEndPoint remoteEndPoint && !remoteEndPoint.Equals(expectedRemoteEndPoint))
                        continue;

                    await connection.InputUdpPacketAsync(new ReadOnlyMemory<byte>(buffer, 0, result.ReceivedBytes), cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private sealed class TestEasyClient : EasyClient
        {
            public TestEasyClient()
                : base(new ConnectionOptions())
            {
            }

            public IConnection CurrentConnection => Connection;

            protected override Task StartReceiveAsync()
            {
                return Task.CompletedTask;
            }
        }
    }
}
