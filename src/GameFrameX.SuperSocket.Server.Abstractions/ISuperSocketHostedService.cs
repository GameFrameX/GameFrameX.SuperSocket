using System;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Primitives;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;
using GameFrameX.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.Hosting;

namespace GameFrameX.SuperSocket.Server.Abstractions
{
    public interface ISuperSocketHostedService : IHostedService, IServer, IConnectionRegister, ILoggerAccessor, ISessionEventHost
    {
    }
}