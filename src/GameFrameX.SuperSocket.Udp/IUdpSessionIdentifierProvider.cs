using System;
using System.Net;

namespace GameFrameX.SuperSocket.Udp
{
    /// <summary>
    /// UDP会话标识符提供者接口
    /// </summary>
    public interface IUdpSessionIdentifierProvider
    {
        /// <summary>
        /// 获取UDP会话的唯一标识符
        /// </summary>
        /// <param name="remoteEndPoint">远程终端点，包含IP地址和端口信息</param>
        /// <param name="data">接收到的UDP数据包内容</param>
        /// <returns>返回会话的唯一标识符字符串</returns>
        string GetSessionIdentifier(IPEndPoint remoteEndPoint, ArraySegment<byte> data);
    }
}