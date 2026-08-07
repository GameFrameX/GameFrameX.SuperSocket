using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.ProtoBase;
using Xunit;

namespace GameFrameX.SuperSocket.Kcp.Tests
{
    /// <summary>
    /// KCP 真实 UDP socket 网络扰动测试。
    /// </summary>
    public class KcpNetworkImpairmentTests
    {
        /// <summary>
        /// 验证本机 KCP 连接在大批量业务消息下保持有序完整交付。
        /// </summary>
        [Fact]
        public async Task Kcp_Over_Udp_Should_Deliver_Large_Traffic_In_Order()
        {
            await using var network = KcpNetwork.Create();
            var messages = CreatePackages(128, 8192);
            var receiveTask = ReceivePayloadPackagesAsync(network.ServerConnection, messages.Count, TimeSpan.FromSeconds(15));

            await SendPayloadPackagesAsync(network.ClientConnection, messages);

            var received = await receiveTask;
            AssertPayloadsEqual(messages, received);
        }

        /// <summary>
        /// 验证 UDP datagram 丢包、重复和乱序时，KCP 仍能恢复有序业务 payload。
        /// </summary>
        [Fact]
        public async Task Kcp_Over_Udp_Should_Recover_From_Packet_Loss_Duplicate_And_Reorder()
        {
            var clientToServer = new NetworkImpairment(lossPercent: 15, duplicatePercent: 10, maxDelayMs: 20, seed: 17);
            var serverToClient = new NetworkImpairment(lossPercent: 8, duplicatePercent: 5, maxDelayMs: 20, seed: 23);
            await using var network = KcpNetwork.Create(clientToServer, serverToClient);
            var messages = CreatePackages(48, 512);
            var receiveTask = ReceivePayloadPackagesAsync(network.ServerConnection, messages.Count, TimeSpan.FromSeconds(20));

            await SendPayloadPackagesAsync(network.ClientConnection, messages);

            var received = await receiveTask;
            AssertPayloadsEqual(messages, received);
            Assert.True(clientToServer.DroppedPackets > 0);
            Assert.True(clientToServer.DelayedPackets > 0);
            Assert.True(clientToServer.DuplicatedPackets > 0);
        }

        /// <summary>
        /// 验证较长时间断网后恢复传输，未确认业务消息能被 KCP 重传交付。
        /// </summary>
        [Fact]
        public async Task Kcp_Over_Udp_Should_Recover_After_Temporary_Network_Blackout()
        {
            var clientToServer = new NetworkImpairment(
                lossPercent: 0,
                duplicatePercent: 0,
                maxDelayMs: 0,
                seed: 31,
                blackoutDuration: TimeSpan.FromSeconds(5));

            await using var network = KcpNetwork.Create(clientToServer, NetworkImpairment.None);
            var messages = CreatePackages(16, 256);
            var receiveTask = ReceivePayloadPackagesAsync(network.ServerConnection, messages.Count, TimeSpan.FromSeconds(30));

            await SendPayloadPackagesAsync(network.ClientConnection, messages);

            var received = await receiveTask;
            AssertPayloadsEqual(messages, received);
            Assert.True(clientToServer.DroppedPackets > 0);
        }

        /// <summary>
        /// 验证分钟级全丢包断网后恢复传输，未确认业务消息能被 KCP 长时间重传交付。
        /// </summary>
        [Fact]
        public async Task Kcp_Over_Udp_Should_Recover_After_Minute_Network_Blackout()
        {
            var clientToServer = new NetworkImpairment(
                lossPercent: 0,
                duplicatePercent: 0,
                maxDelayMs: 0,
                seed: 41,
                blackoutDuration: TimeSpan.FromMinutes(1));

            await using var network = KcpNetwork.Create(
                clientToServer,
                NetworkImpairment.None,
                kcpOptions => kcpOptions.DeadLink = 120);
            var messages = CreatePackages(16, 256);
            var receiveTask = ReceivePayloadPackagesAsync(
                network.ServerConnection,
                messages.Count,
                TimeSpan.FromSeconds(180),
                () => $"client->server dropped={clientToServer.DroppedPackets}, delivered={clientToServer.DeliveredPackets}, lastDeliveredAt={clientToServer.LastDeliveredAt.TotalSeconds:F1}s");

            await SendPayloadPackagesAsync(network.ClientConnection, messages);

            var received = await receiveTask;
            AssertPayloadsEqual(messages, received);
            Assert.True(clientToServer.DroppedPackets > 0);
        }

        /// <summary>
        /// 验证分钟级持续丢包、重复和延迟时，KCP 仍能恢复有序业务 payload。
        /// </summary>
        [Fact]
        public async Task Kcp_Over_Udp_Should_Recover_During_Minute_Packet_Loss()
        {
            var clientToServer = new NetworkImpairment(lossPercent: 20, duplicatePercent: 5, maxDelayMs: 50, seed: 43);
            var serverToClient = new NetworkImpairment(lossPercent: 10, duplicatePercent: 5, maxDelayMs: 50, seed: 47);
            await using var network = KcpNetwork.Create(clientToServer, serverToClient);
            var messages = CreatePackages(60, 256);
            var receiveTask = ReceivePayloadPackagesAsync(network.ServerConnection, messages.Count, TimeSpan.FromSeconds(120));

            await SendPayloadPackagesAsync(network.ClientConnection, messages, TimeSpan.FromSeconds(1));

            var received = await receiveTask;
            AssertPayloadsEqual(messages, received);
            Assert.True(clientToServer.DroppedPackets > 0);
            Assert.True(clientToServer.DelayedPackets > 0);
            Assert.True(clientToServer.DuplicatedPackets > 0);
        }

        private static async Task SendPayloadPackagesAsync(
            KcpPipeConnection connection,
            IReadOnlyList<PayloadPackage> packages,
            TimeSpan interval = default)
        {
            foreach (var package in packages)
            {
                await connection.SendAsync(EncodePackage(package), CancellationToken.None);

                if (interval > TimeSpan.Zero)
                    await Task.Delay(interval).ConfigureAwait(false);
            }
        }

        private static async Task<List<PayloadPackage>> ReceivePayloadPackagesAsync(
            KcpPipeConnection connection,
            int expectedCount,
            TimeSpan timeout,
            Func<string> diagnostics = null)
        {
            using var cts = new CancellationTokenSource(timeout);
            var received = new List<PayloadPackage>(expectedCount);
            var packageEnumerator = connection
                .RunAsync(new PayloadPipelineFilter())
                .GetAsyncEnumerator(cts.Token);

            try
            {
                while (received.Count < expectedCount)
                {
                    var moved = await packageEnumerator
                        .MoveNextAsync()
                        .AsTask()
                        .WaitAsync(cts.Token)
                        .ConfigureAwait(false);

                    Assert.True(moved, $"KCP stream ended after {received.Count}/{expectedCount} packages.");
                    received.Add(packageEnumerator.Current);
                }
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                var suffix = diagnostics == null ? string.Empty : $" {diagnostics()}";
                Assert.Fail($"Timed out after {timeout.TotalSeconds:F0}s receiving KCP packages: {received.Count}/{expectedCount}.{suffix}");
            }
            finally
            {
                try
                {
                    await packageEnumerator.DisposeAsync().ConfigureAwait(false);
                }
                catch (NotSupportedException) when (cts.IsCancellationRequested)
                {
                }
            }

            return received;
        }

        private static List<PayloadPackage> CreatePackages(int count, int baseSize)
        {
            var packages = new List<PayloadPackage>(count);

            for (var i = 0; i < count; i++)
            {
                var size = baseSize + (i % 4) * 1024;
                var body = new byte[size];

                for (var j = 0; j < body.Length; j++)
                {
                    body[j] = (byte)((i + j) & 0xFF);
                }

                packages.Add(new PayloadPackage(i, body, CalculateChecksum(body)));
            }

            return packages;
        }

        private static byte[] EncodePackage(PayloadPackage package)
        {
            var bodyLength = sizeof(int) + sizeof(int) + package.Body.Length;
            var buffer = new byte[sizeof(int) + bodyLength];

            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(0, 4), bodyLength);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(4, 4), package.Sequence);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(8, 4), package.Checksum);
            package.Body.CopyTo(buffer.AsSpan(12));

            return buffer;
        }

        private static void AssertPayloadsEqual(IReadOnlyList<PayloadPackage> expected, IReadOnlyList<PayloadPackage> actual)
        {
            Assert.Equal(expected.Count, actual.Count);

            for (var i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].Sequence, actual[i].Sequence);
                Assert.Equal(expected[i].Checksum, actual[i].Checksum);
                Assert.Equal(expected[i].Checksum, CalculateChecksum(actual[i].Body));
                Assert.Equal(expected[i].Body, actual[i].Body);
            }
        }

        private static int CalculateChecksum(ReadOnlySpan<byte> data)
        {
            unchecked
            {
                var checksum = 17;

                foreach (var value in data)
                {
                    checksum = checksum * 31 + value;
                }

                return checksum;
            }
        }

        private sealed class KcpNetwork : IAsyncDisposable
        {
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private readonly Socket _clientSocket;
            private readonly Socket _serverSocket;
            private readonly Task _clientPump;
            private readonly Task _serverPump;

            private KcpNetwork(
                Socket clientSocket,
                Socket serverSocket,
                KcpPipeConnection clientConnection,
                KcpPipeConnection serverConnection,
                NetworkImpairment clientToServer,
                NetworkImpairment serverToClient)
            {
                _clientSocket = clientSocket;
                _serverSocket = serverSocket;
                ClientConnection = clientConnection;
                ServerConnection = serverConnection;

                var clientEndPoint = (IPEndPoint)_clientSocket.LocalEndPoint;
                var serverEndPoint = (IPEndPoint)_serverSocket.LocalEndPoint;

                _serverPump = PumpSocketAsync(_serverSocket, clientEndPoint, ServerConnection, clientToServer, _cts.Token);
                _clientPump = PumpSocketAsync(_clientSocket, serverEndPoint, ClientConnection, serverToClient, _cts.Token);

                ClientConnection.StartUpdateLoop();
                ServerConnection.StartUpdateLoop();
            }

            public KcpPipeConnection ClientConnection { get; }

            public KcpPipeConnection ServerConnection { get; }

            public static KcpNetwork Create(
                NetworkImpairment clientToServer = null,
                NetworkImpairment serverToClient = null,
                Action<KcpConnectionOptions> configureKcpOptions = null)
            {
                var clientSocket = CreateBoundSocket();
                var serverSocket = CreateBoundSocket();
                var clientEndPoint = (IPEndPoint)clientSocket.LocalEndPoint;
                var serverEndPoint = (IPEndPoint)serverSocket.LocalEndPoint;
                var kcpOptions = new KcpConnectionOptions
                {
                    Conv = 0x87654321,
                    Interval = 1,
                    Resend = 2,
                    NoDelay = true,
                    NoDelayLevel = 1,
                    NoCongestionControl = true,
                    SendWindow = 512,
                    ReceiveWindow = 512,
                    MaxDatagramSize = 4096
                };
                configureKcpOptions?.Invoke(kcpOptions);

                var connectionOptions = new ConnectionOptions
                {
                    MaxPackageLength = 1024 * 1024 * 4
                };

                var clientConnection = new KcpPipeConnection(
                    clientSocket,
                    serverEndPoint,
                    $"client:{kcpOptions.Conv}",
                    kcpOptions.Conv,
                    connectionOptions,
                    kcpOptions);

                var serverConnection = new KcpPipeConnection(
                    serverSocket,
                    clientEndPoint,
                    $"server:{kcpOptions.Conv}",
                    kcpOptions.Conv,
                    connectionOptions,
                    kcpOptions);

                return new KcpNetwork(
                    clientSocket,
                    serverSocket,
                    clientConnection,
                    serverConnection,
                    clientToServer ?? NetworkImpairment.None,
                    serverToClient ?? NetworkImpairment.None);
            }

            public async ValueTask DisposeAsync()
            {
                _cts.Cancel();

                await ClientConnection.CloseAsync(CloseReason.LocalClosing).ConfigureAwait(false);
                await ServerConnection.CloseAsync(CloseReason.LocalClosing).ConfigureAwait(false);

                _clientSocket.Dispose();
                _serverSocket.Dispose();

                await Task.WhenAll(_clientPump, _serverPump).ConfigureAwait(false);
                _cts.Dispose();
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
                NetworkImpairment impairment,
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

                        await impairment
                            .DeliverAsync(connection, buffer.AsMemory(0, result.ReceivedBytes), cancellationToken)
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

                await impairment.DrainAsync().ConfigureAwait(false);
            }
        }

        private sealed class NetworkImpairment
        {
            public static readonly NetworkImpairment None = new NetworkImpairment(0, 0, 0, 0);

            private readonly int _lossPercent;
            private readonly int _duplicatePercent;
            private readonly int _maxDelayMs;
            private readonly Random _random;
            private readonly object _syncRoot = new object();
            private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
            private readonly TimeSpan _blackoutDuration;
            private readonly List<Task> _pendingDeliveries = new List<Task>();

            public NetworkImpairment(
                int lossPercent,
                int duplicatePercent,
                int maxDelayMs,
                int seed,
                TimeSpan blackoutDuration = default)
            {
                _lossPercent = lossPercent;
                _duplicatePercent = duplicatePercent;
                _maxDelayMs = maxDelayMs;
                _random = new Random(seed);
                _blackoutDuration = blackoutDuration;
            }

            public int DroppedPackets { get; private set; }

            public int DeliveredPackets { get; private set; }

            public int DelayedPackets { get; private set; }

            public int DuplicatedPackets { get; private set; }

            public TimeSpan LastDeliveredAt { get; private set; }

            public async ValueTask DeliverAsync(KcpPipeConnection connection, ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken)
            {
                if (_blackoutDuration > TimeSpan.Zero && _stopwatch.Elapsed < _blackoutDuration)
                {
                    DroppedPackets++;
                    return;
                }

                if (Hit(_lossPercent))
                {
                    DroppedPackets++;
                    return;
                }

                await QueueDeliveryAsync(connection, datagram, cancellationToken).ConfigureAwait(false);

                if (!Hit(_duplicatePercent))
                    return;

                DuplicatedPackets++;
                await QueueDeliveryAsync(connection, datagram, cancellationToken).ConfigureAwait(false);
            }

            public Task DrainAsync()
            {
                Task[] deliveries;

                lock (_syncRoot)
                {
                    deliveries = _pendingDeliveries.ToArray();
                }

                return Task.WhenAll(deliveries);
            }

            private async ValueTask QueueDeliveryAsync(KcpPipeConnection connection, ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken)
            {
                var packet = datagram.ToArray();
                var delay = NextDelay();

                if (delay <= 0)
                {
                    MarkDelivered();
                    await connection.InputUdpPacketAsync(packet, cancellationToken).ConfigureAwait(false);
                    return;
                }

                DelayedPackets++;

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested && !connection.IsClosed)
                        {
                            MarkDelivered();
                            await connection.InputUdpPacketAsync(packet, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }, CancellationToken.None);

                lock (_syncRoot)
                {
                    _pendingDeliveries.Add(task);
                }
            }

            private bool Hit(int percent)
            {
                if (percent <= 0)
                    return false;

                lock (_syncRoot)
                {
                    return _random.Next(100) < percent;
                }
            }

            private void MarkDelivered()
            {
                lock (_syncRoot)
                {
                    DeliveredPackets++;
                    LastDeliveredAt = _stopwatch.Elapsed;
                }
            }

            private int NextDelay()
            {
                if (_maxDelayMs <= 0)
                    return 0;

                lock (_syncRoot)
                {
                    return _random.Next(_maxDelayMs + 1);
                }
            }
        }

        private sealed class PayloadPackage
        {
            public PayloadPackage(int sequence, byte[] body, int checksum)
            {
                Sequence = sequence;
                Body = body;
                Checksum = checksum;
            }

            public int Sequence { get; }

            public byte[] Body { get; }

            public int Checksum { get; }
        }

        private sealed class PayloadPipelineFilter : FixedHeaderPipelineFilter<PayloadPackage>
        {
            public PayloadPipelineFilter()
                : base(sizeof(int))
            {
            }

            protected override int GetBodyLengthFromHeader(ref ReadOnlySequence<byte> buffer)
            {
                return BinaryPrimitives.ReadInt32LittleEndian(buffer.FirstSpan.Slice(0, 4));
            }

            protected override PayloadPackage DecodePackage(ref ReadOnlySequence<byte> buffer)
            {
                var data = buffer.ToArray();
                var bodyLength = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0, 4));
                var sequence = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(4, 4));
                var checksum = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(8, 4));
                var body = data.AsSpan(12, bodyLength - 8).ToArray();

                return new PayloadPackage(sequence, body, checksum);
            }
        }
    }
}
