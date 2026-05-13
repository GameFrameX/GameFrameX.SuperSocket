using System;
using System.Threading;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Connection;

namespace GameFrameX.SuperSocket.Kcp
{
    /// <summary>
    /// KCP 连接工厂。创建 KcpPipeConnection 实例。
    /// </summary>
    public class KcpConnectionFactory : IConnectionFactory
    {
        /// <summary>
        /// 创建 KCP 连接。
        /// </summary>
        /// <param name="connection">连接信息（KcpConnectionInfo）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>KCP 连接实例</returns>
        public Task<IConnection> CreateConnection(object connection, CancellationToken cancellationToken)
        {
            var connectionInfo = (KcpConnectionInfo)connection;
            var kcpConnection = new KcpPipeConnection(
                connectionInfo.Socket,
                connectionInfo.RemoteEndPoint,
                connectionInfo.SessionIdentifier,
                connectionInfo.ConnectionOptions,
                connectionInfo.KcpOptions);

            // 启动 KCP Update 循环
            kcpConnection.StartUpdateLoop();

            return Task.FromResult<IConnection>(kcpConnection);
        }
    }
}
