using System.Net.Sockets;

namespace GameFrameX.SuperSocket.Connection
{
    public static class SocketExtensions
    {
        public static bool IsIgnorableSocketException(this SocketException se)
        {
            switch (se.SocketErrorCode)
            {
                case (SocketError.OperationAborted):
                case (SocketError.ConnectionReset):
                case (SocketError.ConnectionAborted):
                case (SocketError.TimedOut):
                case (SocketError.NetworkReset):
                    return true;
                default:
                    return false;
            }
        }
    }
}