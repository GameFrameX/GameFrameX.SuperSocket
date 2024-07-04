using System.Net.Sockets;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;

namespace GameFrameX.SuperSocket.Server.Connection
{
    public class NetworkStreamInitializer : IConnectionStreamInitializer
    {
        public void Setup(ListenOptions listenOptions)
        {
        }

        public Task<Stream> InitializeAsync(Socket socket, Stream stream, CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream>(new NetworkStream(socket, true));
        }
    }
}