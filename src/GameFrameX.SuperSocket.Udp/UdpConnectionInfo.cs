using System;
using System.Net;
using System.Net.Sockets;
using GameFrameX.SuperSocket.Connection;

namespace GameFrameX.SuperSocket.Udp
{
    /// <summary>
    /// UDP连接信息结构体
    /// </summary>
    internal struct UdpConnectionInfo
    {
        /// <summary>
        /// 获取或设置Socket对象
        /// </summary>
        public Socket Socket { get; set; }

        /// <summary>
        /// 获取或设置连接配置选项
        /// </summary>
        public ConnectionOptions ConnectionOptions { get; set; }

        /// <summary>
        /// 获取或设置会话标识符
        /// </summary>
        public string SessionIdentifier { get; set; }

        /// <summary>
        /// 获取或设置远程终结点
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }
    }
}