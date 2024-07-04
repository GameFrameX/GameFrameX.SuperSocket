using System;
using System.Net.Sockets;

namespace GameFrameX.SuperSocket.ClientEngine
{
    public class ProxyEventArgs : EventArgs
    {
        public ProxyEventArgs(Socket socket) : this(true, socket, null, null)
        {
        }

        public ProxyEventArgs(Socket socket, string targetHostName) : this(true, socket, targetHostName, null)
        {
        }

        public ProxyEventArgs(Exception exception) : this(false, null, null, exception)
        {
        }

        public ProxyEventArgs(bool connected, Socket socket, string targetHostName, Exception exception)
        {
            this.Connected = connected;
            this.Socket = socket;
            this.TargetHostName = targetHostName;
            this.Exception = exception;
        }


        public bool Connected { get; private set; }

        public Socket Socket { get; private set; }

        public Exception Exception { get; private set; }

        public string TargetHostName { get; private set; }
    }
}