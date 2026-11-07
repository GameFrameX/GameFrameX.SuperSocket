using System.Net;
using System.Net.Sockets;
using GameFrameX.SuperSocket.ProtoBase;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Session;
using GameFrameX.SuperSocket.Server.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace GameFrameX.SuperSocket.Kcp.Tests;

/// <summary>
/// 多服务器宿主下的 KCP 监听绑定回归测试。
/// <para>
/// 背景：.NET 10 起 <c>AddHostedService</c> 改为 <c>TryAddEnumerable</c> 语义，同 ServiceType + 同实现形态的第二次注册被静默去重。
/// TCP 与 KCP 两个适配器均以 <c>SuperSocketService&lt;TReceivePackage&gt;</c> 工厂注册宿主服务，KCP（后注册）被整体丢弃：
/// 日志宣称 KCP 启动成功，但 UDP 端口从未绑定，客户端握手无响应。
/// </para>
/// </summary>
public class KcpHostListenerBindTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private IHost? _host;

    public KcpHostListenerBindTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Multiple_Server_Host_Binds_Both_Tcp_And_Kcp_Listeners()
    {
        var tcpPort = GetFreePort(SocketType.Stream);
        var udpPort = GetFreePort(SocketType.Dgram);
        _output.WriteLine($"tcp: {tcpPort}, udp: {udpPort}");

        var builder = MultipleServerHostBuilder.Create();

        builder.AddServer<StringPackageInfo, CommandLinePipelineFilter>(b =>
        {
            b.UseClearIdleSession()
             .UseSessionHandler(s => ValueTask.CompletedTask, (s, r) => ValueTask.CompletedTask)
             .UsePackageHandler((s, p) => ValueTask.CompletedTask)
             .UseInProcSessionContainer()
             .ConfigureServices((ctx, services) => services.Configure<ServerOptions>(o =>
                 (o.Listeners ??= new List<ListenOptions>()).Add(new ListenOptions { Ip = "Any", Port = tcpPort })));
        });

        builder.AddServer<StringPackageInfo, CommandLinePipelineFilter>(b =>
        {
            b.UseKcp()
             .UseClearIdleSession()
             .UseSessionHandler(s => ValueTask.CompletedTask, (s, r) => ValueTask.CompletedTask)
             .UsePackageHandler((s, p) => ValueTask.CompletedTask)
             .UseInProcSessionContainer()
             .ConfigureServices((ctx, services) => services.Configure<ServerOptions>(o =>
                 (o.Listeners ??= new List<ListenOptions>()).Add(new ListenOptions { Ip = "Any", Port = udpPort })));
        });

        _host = builder.Build();
        await _host.StartAsync();

        var hostedServices = _host.Services.GetServices<IHostedService>().ToList();
        _output.WriteLine($"hosted services: {hostedServices.Count}");

        // 回归断言 1：两个服务器都必须注册为宿主服务（修复前 TCP 会把 KCP 挤掉，只剩 1 个）
        Assert.True(hostedServices.Count >= 2, $"期望至少 2 个宿主服务（TCP + KCP），实际 {hostedServices.Count}");

        // 回归断言 2：KCP 的 UDP 端口必须真实绑定（修复前仅日志宣称成功，端口无人监听）
        Assert.True(IsPortBound(SocketType.Dgram, udpPort), $"UDP {udpPort} 未被服务器绑定：KCP 监听未真实生效");
        _output.WriteLine($"udp {udpPort} bound: true");
    }

    private static bool IsPortBound(SocketType socketType, int port)
    {
        try
        {
            using var probe = new Socket(AddressFamily.InterNetwork, socketType,
                socketType == SocketType.Stream ? ProtocolType.Tcp : ProtocolType.Udp);
            probe.Bind(new IPEndPoint(IPAddress.Loopback, port));
            return false; // 探针绑定成功 = 服务器没占住端口
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            return true;
        }
    }

    private static int GetFreePort(SocketType socketType)
    {
        using var probe = new Socket(AddressFamily.InterNetwork, socketType,
            socketType == SocketType.Stream ? ProtocolType.Tcp : ProtocolType.Udp);
        probe.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)probe.LocalEndPoint!).Port;
    }

    public void Dispose()
    {
        if (_host != null)
        {
            _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            (_host as IDisposable)?.Dispose();
        }
    }
}
