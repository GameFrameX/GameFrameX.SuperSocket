using System;
using System.Net;
using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;

namespace GameFrameX.SuperSocket.Udp
{
    /// <summary>
    /// UDP连接工厂构建器类
    /// </summary>
    public class UdpConnectionFactoryBuilder : IConnectionFactoryBuilder
    {
        /// <summary>
        /// 构建UDP连接工厂
        /// </summary>
        /// <param name="listenOptions">监听选项</param>
        /// <param name="connectionOptions">连接选项</param>
        /// <returns>返回一个新的UDP连接工厂实例</returns>
        public IConnectionFactory Build(ListenOptions listenOptions, ConnectionOptions connectionOptions)
        {
            return new UdpConnectionFactory();
        }
    }
}