using GameFrameX.SuperSocket.Server.Abstractions.Connections;
using GameFrameX.SuperSocket.Server.Abstractions.Host;
using GameFrameX.SuperSocket.Server.Abstractions.Middleware;
using GameFrameX.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameFrameX.SuperSocket.Udp
{
    /// <summary>
    /// UDP服务器主机构建器扩展类
    /// </summary>
    public static class UdpServerHostBuilderExtensions
    {
        /// <summary>
        /// 使用UDP协议配置SuperSocket主机构建器
        /// </summary>
        /// <param name="hostBuilder">SuperSocket主机构建器</param>
        /// <returns>配置后的SuperSocket主机构建器</returns>
        public static ISuperSocketHostBuilder UseUdp(this ISuperSocketHostBuilder hostBuilder)
        {
            return (hostBuilder.ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IConnectionListenerFactory, UdpConnectionListenerFactory>();
                    services.AddSingleton<IConnectionFactoryBuilder, UdpConnectionFactoryBuilder>();
                }) as ISuperSocketHostBuilder)
                .ConfigureSupplementServices((context, services) =>
                {
                    if (!services.Any(s => s.ServiceType == typeof(IUdpSessionIdentifierProvider)))
                    {
                        services.AddSingleton<IUdpSessionIdentifierProvider, IPAddressUdpSessionIdentifierProvider>();
                    }

                    if (!services.Any(s => s.ServiceType == typeof(IAsyncSessionContainer)))
                    {
                        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMiddleware, InProcSessionContainerMiddleware>(s => s.GetRequiredService<InProcSessionContainerMiddleware>()));
                        services.AddSingleton<InProcSessionContainerMiddleware>();
                        services.AddSingleton<ISessionContainer>((s) => s.GetRequiredService<InProcSessionContainerMiddleware>());
                        services.AddSingleton<IAsyncSessionContainer>((s) => s.GetRequiredService<ISessionContainer>().ToAsyncSessionContainer());
                    }
                });
        }

        /// <summary>
        /// 使用UDP协议配置泛型SuperSocket主机构建器
        /// </summary>
        /// <typeparam name="TReceivePackage">接收数据包的类型</typeparam>
        /// <param name="hostBuilder">泛型SuperSocket主机构建器</param>
        /// <returns>配置后的泛型SuperSocket主机构建器</returns>
        public static ISuperSocketHostBuilder<TReceivePackage> UseUdp<TReceivePackage>(this ISuperSocketHostBuilder<TReceivePackage> hostBuilder)
        {
            return (hostBuilder as ISuperSocketHostBuilder).UseUdp() as ISuperSocketHostBuilder<TReceivePackage>;
        }
    }
}