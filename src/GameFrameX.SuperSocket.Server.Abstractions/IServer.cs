using System;
using System.Threading.Tasks;

namespace GameFrameX.SuperSocket.Server.Abstractions
{
    /// <summary>
    /// 服务器接口，定义了服务器的基本操作
    /// </summary>
    public interface IServer : IServerInfo, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// 异步启动服务器
        /// </summary>
        /// <returns>返回一个Task，表示异步操作。如果启动成功返回true，否则返回false</returns>
        Task<bool> StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步停止服务器
        /// </summary>
        /// <returns>返回一个Task，表示异步操作的完成</returns>
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}