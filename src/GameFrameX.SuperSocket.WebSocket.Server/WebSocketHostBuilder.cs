﻿using GameFrameX.SuperSocket.Server;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;
using GameFrameX.SuperSocket.Server.Abstractions.Session;
using GameFrameX.SuperSocket.Server.Connection;
using GameFrameX.SuperSocket.Server.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace GameFrameX.SuperSocket.WebSocket.Server;

internal class WebSocketHostBuilderAdapter : ServerHostBuilderAdapter<WebSocketPackage>
{
    public WebSocketHostBuilderAdapter(IHostBuilder hostBuilder)
        : base(hostBuilder)
    {
        this.UsePipelineFilter<WebSocketPipelineFilter>();
        this.UseWebSocketMiddleware();
        this.ConfigureServices((ctx, services) => { services.AddSingleton<IPackageHandler<WebSocketPackage>, WebSocketPackageHandler>(); });
        this.ConfigureSupplementServices(WebSocketHostBuilder.ValidateHostBuilder);
    }

    protected override void RegisterDefaultServices(HostBuilderContext builderContext, IServiceCollection servicesInHost, IServiceCollection services)
    {
        services.TryAddSingleton<ISessionFactory, GenericSessionFactory<WebSocketSession>>();
        services.TryAddSingleton<IConnectionListenerFactory, TcpConnectionListenerFactory>();
        services.TryAddSingleton<SocketOptionsSetter>(new SocketOptionsSetter(socket => { }));
        services.TryAddSingleton<IConnectionFactoryBuilder, ConnectionFactoryBuilder>();
        services.TryAddSingleton<IConnectionStreamInitializersFactory, DefaultConnectionStreamInitializersFactory>();
    }
}

public class WebSocketHostBuilder : SuperSocketHostBuilder<WebSocketPackage>
{
    internal WebSocketHostBuilder()
        : this(args: null)
    {
    }

    internal WebSocketHostBuilder(IHostBuilder hostBuilder)
        : base(hostBuilder)
    {
    }

    internal WebSocketHostBuilder(string[] args)
        : base(args)
    {
        this.ConfigureSupplementServices(WebSocketHostBuilder.ValidateHostBuilder);
    }

    protected override void RegisterDefaultServices(HostBuilderContext builderContext, IServiceCollection servicesInHost, IServiceCollection services)
    {
        services.TryAddSingleton<ISessionFactory, GenericSessionFactory<WebSocketSession>>();
        base.RegisterDefaultServices(builderContext, servicesInHost, services);
    }

    public static WebSocketHostBuilder Create()
    {
        return Create(args: null);
    }

    public static WebSocketHostBuilder Create(string[] args)
    {
        return Create(new WebSocketHostBuilder(args));
    }

    public static WebSocketHostBuilder Create(IHostBuilder hostBuilder)
    {
        return Create(new WebSocketHostBuilder(hostBuilder));
    }

    public static WebSocketHostBuilder Create(SuperSocketHostBuilder<WebSocketPackage> hostBuilder)
    {
        return hostBuilder.UsePipelineFilter<WebSocketPipelineFilter>()
                          .UseWebSocketMiddleware()
                          .ConfigureServices((ctx, services) => { services.AddSingleton<IPackageHandler<WebSocketPackage>, WebSocketPackageHandler>(); }) as WebSocketHostBuilder;
    }

    internal static void ValidateHostBuilder(HostBuilderContext builderCtx, IServiceCollection services)
    {
    }
}