using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Client;
using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Primitives;

namespace GameFrameX.SuperSocket.Kcp;

/// <summary>
/// KCP 客户端扩展方法。
/// </summary>
public static class KcpEasyClientExtensions
{
    /// <summary>
    /// 将 <see cref="EasyClient"/> 切换到 KCP 传输。
    /// </summary>
    /// <param name="client">客户端实例。</param>
    /// <param name="remoteEndPoint">远端 UDP 端点。</param>
    /// <param name="kcpOptions">KCP 配置选项。</param>
    /// <param name="bufferPool">接收缓冲池。</param>
    /// <param name="bufferSize">接收缓冲大小。</param>
    public static void AsKcp(
        this EasyClient client,
        IPEndPoint remoteEndPoint,
        KcpConnectionOptions kcpOptions = null,
        ArrayPool<byte> bufferPool = null,
        int bufferSize = 4096)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (remoteEndPoint == null)
        {
            throw new ArgumentNullException(nameof(remoteEndPoint));
        }

        kcpOptions ??= new KcpConnectionOptions();

        if (kcpOptions.Conv == 0)
        {
            kcpOptions.Conv = CreateConv();
        }

        if (client.LocalEndPoint is not IPEndPoint localEndPoint)
        {
            localEndPoint = remoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6 ? new IPEndPoint(IPAddress.IPv6Any, 0) : new IPEndPoint(IPAddress.Any, 0);
        }

        var socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(localEndPoint);

        var connection = new KcpPipeConnection(
            socket,
            remoteEndPoint,
            $"{remoteEndPoint.Address}:{remoteEndPoint.Port}:{kcpOptions.Conv}",
            kcpOptions.Conv,
            client.Options,
            kcpOptions,
            true);

        client.SetupConnection(connection);
        client.LocalEndPoint = socket.LocalEndPoint;

        connection.StartUpdateLoop();
        ReceiveAsync(client, socket, connection, bufferPool, bufferSize, kcpOptions).DoNotAwait();
    }

    private static async Task ReceiveAsync(
        EasyClient client,
        Socket socket,
        KcpPipeConnection connection,
        ArrayPool<byte> bufferPool,
        int bufferSize,
        KcpConnectionOptions kcpOptions)
    {
        bufferPool ??= ArrayPool<byte>.Shared;

        if (bufferSize <= 0)
        {
            bufferSize = kcpOptions?.MaxDatagramSize.GetValueOrDefault() > 0
                             ? kcpOptions.MaxDatagramSize.Value
                             : (int)((kcpOptions?.Mtu ?? Kcp.KcpConstants.IKCP_MTU_DEF) + Kcp.KcpConstants.IKCP_OVERHEAD);
        }

        var receiveEndPoint = connection.RemoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6
                                  ? new IPEndPoint(IPAddress.IPv6Any, 0)
                                  : new IPEndPoint(IPAddress.Any, 0);

        while (!connection.IsClosed)
        {
            var buffer = bufferPool.Rent(bufferSize);

            try
            {
                var result = await socket
                                   .ReceiveFromAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), SocketFlags.None, receiveEndPoint)
                                   .ConfigureAwait(false);

                if (result.ReceivedBytes <= 0)
                {
                    continue;
                }

                if (result.RemoteEndPoint is IPEndPoint remoteEndPoint && !remoteEndPoint.Equals(connection.RemoteEndPoint))
                {
                    continue;
                }

                await connection
                    .InputUdpPacketAsync(new ReadOnlyMemory<byte>(buffer, 0, result.ReceivedBytes), connection.ConnectionToken)
                    .ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (NullReferenceException)
            {
                break;
            }
            catch (Exception e)
            {
                client.OnError("Failed to receive KCP data.", e);
            }
            finally
            {
                bufferPool.Return(buffer);
            }
        }
    }

    private static uint CreateConv()
    {
        Span<byte> buffer = stackalloc byte[4];
        RandomNumberGenerator.Fill(buffer);

        var conv = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        return conv == 0 ? 1 : conv;
    }
}
