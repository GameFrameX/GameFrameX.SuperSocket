using System;
using System.Threading.Tasks;

namespace GameFrameX.SuperSocket.Server.Abstractions
{
    public interface IServer : IServerInfo, IDisposable, IAsyncDisposable
    {
        Task<bool> StartAsync();

        Task StopAsync();
    }
}