using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Server.Abstractions.Session;

namespace GameFrameX.SuperSocket.Server
{
    public class SessionHandlers
    {
        public Func<IAppSession, ValueTask> Connected { get; set; }

        public Func<IAppSession, CloseEventArgs, ValueTask> Closed { get; set; }
    }
}