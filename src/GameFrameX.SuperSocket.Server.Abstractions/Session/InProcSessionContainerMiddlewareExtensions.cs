using GameFrameX.SuperSocket.Server.Abstractions.Host;
using Microsoft.Extensions.DependencyInjection;

namespace GameFrameX.SuperSocket.Server.Abstractions.Session
{
    public static class InProcSessionContainerMiddlewareExtensions
    {
        public static ISuperSocketHostBuilder UseInProcSessionContainer(this ISuperSocketHostBuilder builder)
        {
            return builder
                .UseMiddleware<InProcSessionContainerMiddleware>(s => s.GetRequiredService<InProcSessionContainerMiddleware>())
                .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton<InProcSessionContainerMiddleware>();
                    services.AddSingleton<ISessionContainer>((s) => s.GetRequiredService<InProcSessionContainerMiddleware>());
                    services.AddSingleton<IAsyncSessionContainer>((s) => s.GetRequiredService<ISessionContainer>().ToAsyncSessionContainer());
                }) as ISuperSocketHostBuilder;
        }
    }
}