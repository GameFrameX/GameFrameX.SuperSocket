using GameFrameX.SuperSocket.Connection;
using Microsoft.Extensions.Logging;

namespace GameFrameX.SuperSocket.Server.Abstractions.Connections
{
    public interface IConnectionListenerFactory
    {
        IConnectionListener CreateConnectionListener(ListenOptions options, ConnectionOptions connectionOptions, ILoggerFactory loggerFactory);
    }
}