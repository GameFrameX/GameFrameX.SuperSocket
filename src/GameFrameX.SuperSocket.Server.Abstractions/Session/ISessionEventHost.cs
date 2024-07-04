using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Connection;

namespace GameFrameX.SuperSocket.Server.Abstractions.Session
{
    public interface ISessionEventHost
    {
        ValueTask HandleSessionConnectedEvent(IAppSession session);

        ValueTask HandleSessionClosedEvent(IAppSession session, CloseReason reason);
    }
}