using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;

namespace GameFrameX.SuperSocket.Kcp
{
    /// <summary>
    /// 构建 KCP 连接工厂。
    /// </summary>
    public class KcpConnectionFactoryBuilder : IConnectionFactoryBuilder
    {
        /// <summary>
        /// 根据监听选项和连接选项构建 KCP 连接工厂。
        /// </summary>
        /// <param name="listenOptions">监听选项</param>
        /// <param name="connectionOptions">连接选项</param>
        /// <returns>KCP 连接工厂</returns>
        public IConnectionFactory Build(ListenOptions listenOptions, ConnectionOptions connectionOptions)
        {
            return new KcpConnectionFactory();
        }
    }
}
