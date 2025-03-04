using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Connection;

namespace GameFrameX.SuperSocket.Udp
{
    /// <summary>
    /// UDP连接工厂类，用于创建UDP连接
    /// </summary>
    public class UdpConnectionFactory : IConnectionFactory
    {
        /// <summary>
        /// 创建一个新的UDP连接
        /// </summary>
        /// <param name="connection">连接信息对象，需要是UdpConnectionInfo类型</param>
        /// <param name="cancellationToken">取消令牌，用于取消连接创建操作</param>
        /// <returns>返回一个表示异步操作的Task，其结果为IConnection接口的实现</returns>
        /// <remarks>
        /// 此方法将传入的connection对象转换为UdpConnectionInfo，并使用其中的信息创建一个新的UdpPipeConnection
        /// </remarks>
        public Task<IConnection> CreateConnection(object connection, CancellationToken cancellationToken)
        {
            var connectionInfo = (UdpConnectionInfo)connection;

            return Task.FromResult<IConnection>(new UdpPipeConnection(connectionInfo.Socket, connectionInfo.ConnectionOptions, connectionInfo.RemoteEndPoint, connectionInfo.SessionIdentifier));
        }
    }
}