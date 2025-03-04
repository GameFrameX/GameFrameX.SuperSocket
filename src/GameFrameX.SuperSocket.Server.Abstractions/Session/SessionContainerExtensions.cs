using System;
using System.Linq;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Server.Abstractions.Middleware;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace GameFrameX.SuperSocket.Server.Abstractions.Session
{
    /// <summary>
    /// 会话容器扩展类
    /// </summary>
    public static class SessionContainerExtensions
    {
        /// <summary>
        /// 将异步会话容器转换为同步会话容器
        /// </summary>
        /// <param name="asyncSessionContainer">异步会话容器</param>
        /// <returns>同步会话容器</returns>
        public static ISessionContainer ToSyncSessionContainer(this IAsyncSessionContainer asyncSessionContainer)
        {
            return new AsyncToSyncSessionContainerWrapper(asyncSessionContainer);
        }

        /// <summary>
        /// 将同步会话容器转换为异步会话容器
        /// </summary>
        /// <param name="syncSessionContainer">同步会话容器</param>
        /// <returns>异步会话容器</returns>
        public static IAsyncSessionContainer ToAsyncSessionContainer(this ISessionContainer syncSessionContainer)
        {
            return new SyncToAsyncSessionContainerWrapper(syncSessionContainer);
        }

        /// <summary>
        /// 从服务提供者获取同步会话容器
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        /// <returns>同步会话容器</returns>
        [Obsolete("Please use the method server.GetSessionContainer() instead.")]
        public static ISessionContainer GetSessionContainer(this IServiceProvider serviceProvider)
        {
            var sessionContainer = serviceProvider.GetServices<IMiddleware>()
                .OfType<ISessionContainer>()
                .FirstOrDefault();

            if (sessionContainer != null)
                return sessionContainer;

            var asyncSessionContainer = serviceProvider.GetServices<IMiddleware>()
                .OfType<IAsyncSessionContainer>()
                .FirstOrDefault();

            return asyncSessionContainer?.ToSyncSessionContainer();
        }

        /// <summary>
        /// 从服务提供者获取异步会话容器
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        /// <returns>异步会话容器</returns>
        [Obsolete("Please use the method server.GetSessionContainer() instead.")]
        public static IAsyncSessionContainer GetAsyncSessionContainer(this IServiceProvider serviceProvider)
        {
            var asyncSessionContainer = serviceProvider.GetServices<IMiddleware>()
                .OfType<IAsyncSessionContainer>()
                .FirstOrDefault();

            if (asyncSessionContainer != null)
                return asyncSessionContainer;

            var sessionContainer = serviceProvider.GetServices<IMiddleware>()
                .OfType<ISessionContainer>()
                .FirstOrDefault();

            return sessionContainer?.ToAsyncSessionContainer();
        }

        /// <summary>
        /// 从服务器信息获取同步会话容器
        /// </summary>
        /// <param name="server">服务器信息</param>
        /// <returns>同步会话容器</returns>
        public static ISessionContainer GetSessionContainer(this IServerInfo server)
        {
#pragma warning disable CS0618
            return server.ServiceProvider.GetSessionContainer();
#pragma warning restore CS0618
        }

        /// <summary>
        /// 从服务器信息获取异步会话容器
        /// </summary>
        /// <param name="server">服务器信息</param>
        /// <returns>异步会话容器</returns>
        public static IAsyncSessionContainer GetAsyncSessionContainer(this IServerInfo server)
        {
#pragma warning disable CS0618
            return server.ServiceProvider.GetAsyncSessionContainer();
#pragma warning restore CS0618
        }
    }
}