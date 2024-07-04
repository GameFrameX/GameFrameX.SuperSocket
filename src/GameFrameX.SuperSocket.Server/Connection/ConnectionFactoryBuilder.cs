using System.Net.Sockets;
using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;

namespace GameFrameX.SuperSocket.Server.Connection
{
    public class ConnectionFactoryBuilder : IConnectionFactoryBuilder
    {
        public Action<Socket> SocketOptionsSetter { get; }

        public IConnectionStreamInitializersFactory ConnectionStreamInitializersFactory { get; }

        public ConnectionFactoryBuilder(SocketOptionsSetter socketOptionsSetter, IConnectionStreamInitializersFactory connectionStreamInitializersFactory)
        {
            SocketOptionsSetter = socketOptionsSetter.Setter;
            ConnectionStreamInitializersFactory = connectionStreamInitializersFactory;
        }

        public virtual IConnectionFactory Build(ListenOptions listenOptions, ConnectionOptions connectionOptions)
        {
            return new TcpConnectionFactory(listenOptions, connectionOptions, SocketOptionsSetter, ConnectionStreamInitializersFactory);
        }
    }
}