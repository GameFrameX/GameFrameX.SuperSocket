using System;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;
using GameFrameX.SuperSocket.Server.Abstractions.Host;
using GameFrameX.SuperSocket.Server.Abstractions.Middleware;
using GameFrameX.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GameFrameX.SuperSocket.Kcp
{
    /// <summary>
    /// 提供 KCP 协议的 HostBuilder 扩展方法。
    /// </summary>
    public static class KcpServerHostBuilderExtensions
    {
        /// <summary>
        /// 配置 HostBuilder 使用 KCP 协议（默认参数）。
        /// </summary>
        /// <param name="hostBuilder">Host 构建器</param>
        /// <returns>配置后的 Host 构建器</returns>
        public static ISuperSocketHostBuilder UseKcp(this ISuperSocketHostBuilder hostBuilder)
        {
            return hostBuilder.UseKcp(_ => { });
        }

        /// <summary>
        /// 配置 HostBuilder 使用 KCP 协议（自定义参数）。
        /// </summary>
        /// <param name="hostBuilder">Host 构建器</param>
        /// <param name="configure">KCP 配置委托</param>
        /// <returns>配置后的 Host 构建器</returns>
        public static ISuperSocketHostBuilder UseKcp(
            this ISuperSocketHostBuilder hostBuilder,
            Action<KcpConnectionOptions> configure)
        {
            return (hostBuilder.ConfigureServices((context, services) =>
            {
                services.AddSingleton<IConnectionListenerFactory, KcpConnectionListenerFactory>();
                services.AddSingleton<IConnectionFactoryBuilder, KcpConnectionFactoryBuilder>();
                services.Configure(configure);
            }) as ISuperSocketHostBuilder)
            .ConfigureSupplementServices((context, services) =>
            {
                if (!services.Any(s => s.ServiceType == typeof(IKcpSessionIdentifierProvider)))
                {
                    services.AddSingleton<IKcpSessionIdentifierProvider, KcpConvIdentifierProvider>();
                }

                if (!services.Any(s => s.ServiceType == typeof(IAsyncSessionContainer)))
                {
                    services.TryAddEnumerable(ServiceDescriptor.Singleton<IMiddleware, InProcSessionContainerMiddleware>(
                        s => s.GetRequiredService<InProcSessionContainerMiddleware>()));
                    services.AddSingleton<InProcSessionContainerMiddleware>();
                    services.AddSingleton<ISessionContainer>((s) => s.GetRequiredService<InProcSessionContainerMiddleware>());
                    services.AddSingleton<IAsyncSessionContainer>((s) => s.GetRequiredService<ISessionContainer>().ToAsyncSessionContainer());
                }
            });
        }

        /// <summary>
        /// 配置泛型 HostBuilder 使用 KCP 协议（默认参数）。
        /// </summary>
        /// <typeparam name="TReceivePackage">接收包类型</typeparam>
        /// <param name="hostBuilder">泛型 Host 构建器</param>
        /// <returns>配置后的 Host 构建器</returns>
        public static ISuperSocketHostBuilder<TReceivePackage> UseKcp<TReceivePackage>(
            this ISuperSocketHostBuilder<TReceivePackage> hostBuilder)
        {
            return (hostBuilder as ISuperSocketHostBuilder).UseKcp() as ISuperSocketHostBuilder<TReceivePackage>;
        }

        /// <summary>
        /// 配置泛型 HostBuilder 使用 KCP 协议（自定义参数）。
        /// </summary>
        /// <typeparam name="TReceivePackage">接收包类型</typeparam>
        /// <param name="hostBuilder">泛型 Host 构建器</param>
        /// <param name="configure">KCP 配置委托</param>
        /// <returns>配置后的 Host 构建器</returns>
        public static ISuperSocketHostBuilder<TReceivePackage> UseKcp<TReceivePackage>(
            this ISuperSocketHostBuilder<TReceivePackage> hostBuilder,
            Action<KcpConnectionOptions> configure)
        {
            return (hostBuilder as ISuperSocketHostBuilder).UseKcp(configure)
                as ISuperSocketHostBuilder<TReceivePackage>;
        }
    }
}
