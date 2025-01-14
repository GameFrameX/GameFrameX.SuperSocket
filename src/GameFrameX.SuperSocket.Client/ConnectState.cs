using System.Net.Sockets;
using GameFrameX.SuperSocket.Connection;
using Microsoft.Extensions.ObjectPool;

namespace GameFrameX.SuperSocket.Client
{
    /// <summary>
    /// 连接状态类，用于管理和存储连接相关的信息
    /// </summary>
    public class ConnectState
    {
        /// <summary>
        /// 初始化连接状态实例
        /// </summary>
        public ConnectState()
        {
        }

        /// <summary>
        /// 使用指定的取消状态初始化连接状态实例
        /// </summary>
        /// <param name="cancelled">是否已取消</param>
        private ConnectState(bool cancelled)
        {
            Cancelled = cancelled;
        }

        /// <summary>
        /// 获取或设置连接结果
        /// </summary>
        public bool Result { get; set; }

        /// <summary>
        /// 获取连接是否已被取消
        /// </summary>
        public bool Cancelled { get; private set; }

        /// <summary>
        /// 获取或设置连接过程中发生的异常
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// 获取或设置Socket对象
        /// </summary>
        public Socket Socket { get; set; }

        /// <summary>
        /// 获取或设置数据流对象
        /// </summary>
        public Stream Stream { get; set; }

        /// <summary>
        /// 表示已取消的连接状态的静态实例
        /// </summary>
        public static readonly ConnectState CancelledState = new ConnectState(false);

        private static Lazy<ObjectPool<SocketSender>> _socketSenderPool = new Lazy<ObjectPool<SocketSender>>(() =>
        {
            var policy = new DefaultPooledObjectPolicy<SocketSender>();
            var pool = new DefaultObjectPool<SocketSender>(policy, EasyClient.SocketSenderPoolSzie ?? EasyClient.DefaultSocketSenderPoolSzie);
            return pool;
        });
        /// <summary>
        /// 根据当前连接状态创建新的连接实例
        /// </summary>
        /// <param name="connectionOptions">连接配置选项</param>
        /// <returns>返回创建的IConnection接口实例</returns>
        public IConnection CreateConnection(ConnectionOptions connectionOptions)
        {
            var stream = this.Stream;
            var socket = this.Socket;

            if (stream != null)
            {
                return new StreamPipeConnection(stream, socket.RemoteEndPoint, socket.LocalEndPoint, connectionOptions);
            }
            else
            {
                return new TcpPipeConnection(socket, connectionOptions, _socketSenderPool.Value);
            }
        }
    }
}