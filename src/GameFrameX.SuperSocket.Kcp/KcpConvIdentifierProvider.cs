using System.Buffers.Binary;
using System.Net;
using GameFrameX.SuperSocket.ProtoBase;

namespace GameFrameX.SuperSocket.Kcp;

/// <summary>
/// 基于 KCP Conv 的默认会话标识提供者。
/// 从 UDP 包前 4 字节读取 Conv，拼接远端地址和 Conv 作为唯一标识。
/// </summary>
internal class KcpConvIdentifierProvider : IKcpSessionIdentifierProvider
{
    /// <summary>
    /// 从收到的 UDP 包中提取会话标识。
    /// 标识格式为 "EndPoint:Conv"。
    /// </summary>
    /// <param name="remoteEndPoint">远端地址</param>
    /// <param name="data">UDP 包原始数据</param>
    /// <returns>会话唯一标识</returns>
    /// <exception cref="ArgumentNullException">远端地址为空</exception>
    /// <exception cref="ProtocolException">数据太短无法提取 Conv</exception>
    public string GetSessionIdentifier(EndPoint remoteEndPoint, ReadOnlySpan<byte> data)
    {
        if (remoteEndPoint == null)
        {
            throw new ArgumentNullException(nameof(remoteEndPoint));
        }

        if (data.Length < 4)
        {
            throw new ProtocolException("KCP packet too short to extract Conv");
        }

        var conv = BinaryPrimitives.ReadUInt32LittleEndian(data);
        return $"{FormatRemoteEndPoint(remoteEndPoint)}:{conv}";
    }

    private static string FormatRemoteEndPoint(EndPoint remoteEndPoint)
    {
        switch (remoteEndPoint)
        {
            case IPEndPoint ipEndPoint:
                return ipEndPoint.ToString();
            case DnsEndPoint dnsEndPoint:
                return $"{dnsEndPoint.Host}:{dnsEndPoint.Port}";
            default:
                return remoteEndPoint.ToString();
        }
    }
}
