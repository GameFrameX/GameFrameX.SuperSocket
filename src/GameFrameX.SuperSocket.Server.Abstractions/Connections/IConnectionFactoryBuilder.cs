using GameFrameX.SuperSocket.Connection;

namespace GameFrameX.SuperSocket.Server.Abstractions.Connections
{
    public interface IConnectionFactoryBuilder
    {
        IConnectionFactory Build(ListenOptions listenOptions, ConnectionOptions connectionOptions);
    }
}