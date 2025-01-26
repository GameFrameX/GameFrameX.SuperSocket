using System;
using Microsoft.Extensions.Logging;
using GameFrameX.SuperSocket.ProtoBase;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Connection;

namespace GameFrameX.SuperSocket.Server.Abstractions.Connections
{
    public delegate ValueTask NewConnectionAcceptHandler(ListenOptions listenOptions, IConnection connection);

    public interface IConnectionListener : IDisposable
    {
        ListenOptions Options { get; }

        bool Start();

        event NewConnectionAcceptHandler NewConnectionAccept;

        Task StopAsync();

        bool IsRunning { get; }

        IConnectionFactory ConnectionFactory { get; }
    }
}