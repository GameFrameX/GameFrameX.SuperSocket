using System.Net;

namespace GameFrameX.SuperSocket.Client.Proxy
{
    public class Socks4Connector : ConnectorBase
    {
        protected override ValueTask<ConnectState> ConnectAsync(EndPoint remoteEndPoint, ConnectState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}