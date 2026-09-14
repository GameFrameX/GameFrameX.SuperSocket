<div align="center">

<img src="https://download.alianblank.com/gameframex/gameframex_logo_320.png" alt="Game Frame X Logo" width="160" />

# GameFrameX.SuperSocket

[![License](https://img.shields.io/badge/license-blue.svg)](LICENSE)
[![Version](https://img.shields.io/nuget/v/GameFrameX.SuperSocket.Server)](https://www.nuget.org/packages/GameFrameX.SuperSocket.Server)
[![Documentation](https://img.shields.io/badge/docs-gameframex-brightgreen.svg)](https://gameframex.doc.alianblank.com)

[![Discord](https://img.shields.io/badge/-5865F2?logo=discord&logoColor=white)](https://discord.gg/VDWUjWMDw9)
[![GitHub](https://img.shields.io/badge/-181717?logo=github&logoColor=white)](https://github.com/GameFrameX/gameframex)
[![Bilibili](https://img.shields.io/badge/-00A1D6?logo=bilibili&logoColor=white)](https://www.bilibili.com/video/BV1yrpeepEn7)
[![Gitee](https://img.shields.io/badge/-C71D23?logo=gitee&logoColor=white)](https://gitee.com/GameFrameX/gameframex)

**独立游戏前后端一体化解决方案 · 独立游戏开发者的圆梦大使**

<br />

[文档](https://gameframex.doc.alianblank.com) · [快速开始](#快速开始) · QQ群: 467608841 / 233840761

<br />

[English](README.md) | **简体中文** | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

</div>

## 项目简介

GameFrameX.SuperSocket 是 GameFrameX 维护的 [SuperSocket](https://github.com/kerryjiang/SuperSocket) 分支 —— 一个用纯 C# 编写的轻量级、可扩展的 socket 应用框架。你可以用它轻松构建始终保持连接的 socket 应用，无需关心如何使用 socket、如何维护 socket 连接以及 socket 的工作原理。

上游架构与公开 API 均保持不变，因此针对 SuperSocket 编写的项目依然可用。除此之外，本分支还承载了 GameFrameX 游戏服务器所需的改动：.NET 10 构建目标、DI/构造函数注入支持，以及面向弱网游戏流量的 KCP / ReliableSession 协议适配。

### 功能特性

- 轻量且可扩展 —— 无需手工管理 socket，即可构建始终保持连接的 socket 应用。
- 纯 C# 实现，可集成进任何现有 .NET 系统。
- 带管道过滤器与包解码器的协议解码管道。
- 默认传输为 TCP；UDP、KCP 与 ReliableSession 需显式启用。
- KCP 传输在 UDP 数据报之上提供可靠投递，具备重传与窗口控制。
- ReliableSession 协议帧契约与二进制编解码器，支持逻辑会话恢复、重放游标、确认区间、快照回退以及关闭/错误帧。
- 命令模式的请求处理。
- WebSocket 服务端与客户端，以及 Kestrel 集成。
- 对 DI / 构造函数注入友好的宿主构建器。
- .NET 10 构建目标。

## 快速开始

### 安装

从 NuGet.org 安装你需要的模块：

```bash
dotnet add package GameFrameX.SuperSocket.Server
dotnet add package GameFrameX.SuperSocket.ProtoBase
```

`Kcp` 与 `ReliableSession` 模块将随下个版本发布；在此之前，可从源码检出构建：

```bash
git clone https://github.com/GameFrameX/GameFrameX.SuperSocket.git
cd GameFrameX.SuperSocket
dotnet build GameFrameX.SuperSocket.slnx
```

在同一个源码检出中运行测试套件：

```bash
dotnet test GameFrameX.SuperSocket.slnx
dotnet test test/GameFrameX.SuperSocket.ReliableSession.Tests/GameFrameX.SuperSocket.ReliableSession.Tests.csproj
```

## 使用示例

### 传输选型

TCP 仍是默认传输。UDP、KCP 与 ReliableSession 需要显式选择：

| 选项 | 适用场景 | 当前行为 |
|:---|:---|:---|
| TCP | 需要标准的 SuperSocket 连接路径。 | 默认的服务端/客户端传输。 |
| 原生 UDP | 需要数据报投递，且能自行容忍丢包、重复与乱序。 | 通过 `UseUdp()` / `AsUdp(...)` 显式启用；不可靠的数据报传输。 |
| KCP | 需要在 UDP 数据报之上借助 KCP 的重传/窗口控制实现可靠投递。 | 通过 `UseKcp(...)` / `AsKcp(...)` 显式启用；不是 KCP-over-TCP。 |
| ReliableSession | 需要一套协议契约，覆盖逻辑会话恢复、重放游标、确认区间、快照回退以及关闭/错误帧。 | 仅提供协议模型与二进制编解码器。运行时心跳、恢复状态、重放缓存、去重缓存、适配器与业务投递在 C3 中尚未实现。 |

### 服务端：启用 KCP

引用 `GameFrameX.SuperSocket.Kcp`，保留你原有的包管道与处理器，然后在宿主构建器上添加
`UseKcp(...)`：

```csharp
using System.Text;
using GameFrameX.SuperSocket.Kcp;
using GameFrameX.SuperSocket.ProtoBase;
using GameFrameX.SuperSocket.Server.Host;

var builder = SuperSocketHostBuilder
    .Create<TextPackageInfo, LinePipelineFilter>()
    .UseKcp(options =>
    {
        // 未设置的可空选项保持 KCP 的内部默认值。
        options.NoDelay = true;
        options.NoDelayLevel = 1;
        options.Interval = 10;
        options.Resend = 2;
        options.NoCongestionControl = true;
        options.SendWindow = 512;
        options.ReceiveWindow = 512;
        options.MaxDatagramSize = 4096;

        // 预期出现分钟级丢包黑洞时，请显式调高此项。
        options.DeadLink = 120;
    })
    .UsePackageHandler(async (session, package) =>
    {
        // 与在 TCP 上一样处理解码后的 SuperSocket 包。
        await session.SendAsync(Encoding.UTF8.GetBytes(package.Text + "\r\n"));
    });
```

`UseKcp(...)` 会在尚未注册时注册 KCP 监听器/工厂与默认的进程内会话容器。默认的 KCP 服务端会话标识
由远端端点加上从入站 UDP 包中读取的 KCP `Conv` 共同构成。因此，仅靠 KCP 传输层并不支持
端点/NAT 迁移。

### 客户端：使用 KCP

引用 `GameFrameX.SuperSocket.Kcp`，用 `AsKcp(...)` 配置 `EasyClient`，随后即可在客户端上使用
常规的接收/发送 API：

```csharp
using System.Net;
using System.Text;
using GameFrameX.SuperSocket.Client;
using GameFrameX.SuperSocket.Kcp;
using GameFrameX.SuperSocket.ProtoBase;

var remoteEndPoint = new IPEndPoint(IPAddress.Loopback, 4040);
var client = new EasyClient<TextPackageInfo>(new LinePipelineFilter());

client.AsKcp(remoteEndPoint, new KcpConnectionOptions
{
    // Conv = 0 表示由客户端生成一个非零的会话 id。
    Conv = 0,
    NoDelay = true,
    NoDelayLevel = 1,
    Interval = 10,
    Resend = 2,
    MaxDatagramSize = 4096
});

client.StartReceive();
await ((IEasyClient)client).SendAsync(Encoding.UTF8.GetBytes("ping\r\n"));
```

`AsKcp(...)` 会创建并绑定 UDP socket、分配或生成 `Conv`、创建 `KcpPipeConnection`、
启动 KCP 更新循环，并开始为该连接接收 UDP 包。当客户端必须绑定特定的本地 UDP 端点时，
请在 `AsKcp(...)` 之前设置 `client.LocalEndPoint`。

### KCP 配置说明

- 除非有经过实测的理由需要调优，否则请保持可空选项为未设置状态；未设置的值会沿用 KCP
  的内部默认值。
- 对于实时游戏类流量，常见的起点是 `NoDelay = true`、`NoDelayLevel = 1`、
  `Interval = 10`、`Resend = 2`，以及调校后的发送/接收窗口。
- 若追求保守的吞吐，请保留更多默认值，并避免关闭拥塞控制。
- `DeadLink` 是单个 KCP 分片的最大重传次数。其内部默认值并非分钟级的黑洞容忍策略；
  当验收条件要求更长的黑洞容忍时间时，请有意识地调高它。
- `IdleTimeout` 属于连接生命周期层，它不是逻辑会话恢复窗口。
- `MaxDatagramSize` 应与你的网络 MTU 策略相匹配。过大的 UDP 数据报会提高分片与丢包
  风险。

### ReliableSession 协议模型

需要协议帧契约与二进制编解码器时，请引用 `GameFrameX.SuperSocket.ReliableSession`：

```csharp
using System.Text;
using GameFrameX.SuperSocket.ReliableSession;

var codec = new ReliableSessionFrameCodec();
var sessionId = new SessionId(Guid.NewGuid());

var hello = new ReliableSessionHelloFrame
{
    ClientInstanceId = new ClientInstanceId(Guid.NewGuid()),
    ProtocolVersion = ReliableSessionProtocol.WireVersion,
    RequestedOptions = new ReliableSessionHandshakeOptions
    {
        HeartbeatInterval = TimeSpan.FromSeconds(5),
        HeartbeatTimeout = TimeSpan.FromSeconds(15),
        RecoveryWindow = TimeSpan.FromMinutes(2),
        ReplayWindowSize = 1024
    }
};

var helloBytes = codec.Encode(hello);
var decodedHello = (ReliableSessionHelloFrame)codec.Decode(helloBytes);

var data = new ReliableSessionDataFrame
{
    SessionId = sessionId,
    MessageId = new MessageId(1),
    Sequence = new Sequence(1),
    Payload = Encoding.UTF8.GetBytes("move:1,2")
};

var dataBytes = codec.Encode(data);
var decodedData = (ReliableSessionDataFrame)codec.Decode(dataBytes);

var ack = new ReliableSessionAckFrame
{
    SessionId = sessionId,
    Ranges = new[] { new AckRange(new Sequence(1), new Sequence(1)) }
};

var ackBytes = codec.Encode(ack);
var decodedAck = (ReliableSessionAckFrame)codec.Decode(ackBytes);
```

ReliableSession 目前定义并校验以下帧类型：`Hello`、`HelloAck`、`Resume`、
`ResumeAck`、`Heartbeat`、`Data`、`Ack`、`SnapshotRequest`、`Snapshot`、`Close` 和 `Error`。
编解码器要求每个缓冲区恰好包含一个完整的 ReliableSession 帧；传输层的流式拆包/组帧
属于后续适配器的职责。

当前边界：

- 目前还没有任何服务端/客户端运行时开关可以启用 ReliableSession。
- 尚未包含自动心跳定时器、重连循环、恢复令牌存储、重放缓存、去重缓存或
  快照提供程序。
- KCP 仍以端点加 `Conv` 作为其传输会话标识。ReliableSession 的
  `SessionId` 加 `ResumeToken` 是未来的逻辑会话恢复契约，而不是当前对 KCP
  端点迁移的支持。
- C3 的测试覆盖属于协议/编解码器的端到端覆盖，包括生命周期、10s/30s/60s 黑洞
  恢复脚本、重放、快照回退、重复/乱序帧以及确认区间。它不是运行时传输集成覆盖。

## 架构概览

各模块从协议原语逐层向上直至宿主：

- `Primitives` / `ProtoBase` —— 原语接口与协议解码。
- `Connection` / `Channel` —— 底层通信抽象与请求管道。
- `Server` / `Server.Abstractions` / `Client` / `ClientEngine` / `Client.Proxy` —— 宿主、服务端与客户端端点。
- `Command` —— 构建在服务端之上的命令模式请求处理。
- `Udp` / `Kcp` / `ReliableSession` —— 可选传输与逻辑会话协议。
- `WebSocket` / `WebSocket.Server` / `Kestrel` / `Http` —— HTTP 系协议与宿主托管。

## 平台支持

- .NET 10.0
- Windows、macOS、Linux

## 依赖

| 模块 | 包 | 说明 |
|:---|:---|:---|
| Primitives | `GameFrameX.SuperSocket.Primitives` | 原语接口与类 |
| ProtoBase | `GameFrameX.SuperSocket.ProtoBase` | 协议解码 |
| Connection | `GameFrameX.SuperSocket.Connection` | 带管道的底层通信抽象 |
| Server Abstractions | `GameFrameX.SuperSocket.Server.Abstractions` | 服务端抽象 |
| Server | `GameFrameX.SuperSocket.Server` | 服务端宿主 |
| Client | `GameFrameX.SuperSocket.Client` | 客户端端点 |
| Client Engine | `GameFrameX.SuperSocket.ClientEngine` | 客户端引擎 |
| Client Proxy | `GameFrameX.SuperSocket.Client.Proxy` | 客户端代理支持 |
| Command | `GameFrameX.SuperSocket.Command` | 命令模式请求处理 |
| Udp | `GameFrameX.SuperSocket.Udp` | UDP 传输 |
| Kcp | `GameFrameX.SuperSocket.Kcp` | 基于 UDP 的 KCP 传输 |
| ReliableSession | `GameFrameX.SuperSocket.ReliableSession` | ReliableSession 协议模型与编解码器 |
| WebSocket | `GameFrameX.SuperSocket.WebSocket` | WebSocket 协议实现 |
| WebSocket Server | `GameFrameX.SuperSocket.WebSocket.Server` | WebSocket 服务端 |
| Kestrel | `GameFrameX.SuperSocket.Kestrel` | Kestrel 集成 |
| Http | `GameFrameX.SuperSocket.Http` | 类 HTTP 协议的共享工具 |

除 .NET 基础类库之外，各模块还依赖 `Microsoft.Extensions.*`
（Configuration、DependencyInjection、Hosting、Logging、Options）、`System.IO.Pipelines`，以及
Kestrel 模块所需的 `Microsoft.AspNetCore.App` 框架引用。

## 文档与资源

- [文档](https://gameframex.doc.alianblank.com)
- [GitHub 仓库](https://github.com/GameFrameX/GameFrameX.SuperSocket)
- [问题反馈](https://github.com/GameFrameX/GameFrameX.SuperSocket/issues)
- [上游项目](https://github.com/kerryjiang/SuperSocket)
- [上游文档](https://docs.supersocket.net/)

## 社区与支持

![QQ](https://img.shields.io/badge/QQ-467608841%2F233840761-EB1923?style=for-the-badge&logo=qq&logoColor=white)
[![Bilibili](https://img.shields.io/badge/Bilibili-00A1D6?style=for-the-badge&logo=bilibili&logoColor=white)](https://www.bilibili.com/video/BV1yrpeepEn7)
[![Gitee](https://img.shields.io/badge/Gitee-C71D23?style=for-the-badge&logo=gitee&logoColor=white)](https://gitee.com/GameFrameX/gameframex)
[![GitHub](https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/GameFrameX/gameframex)
[![Discord](https://img.shields.io/badge/Discord-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/VDWUjWMDw9)
[<img src="https://cdn.jsdelivr.net/npm/devicon@2/icons/linkedin/linkedin-original.svg" height="28" alt="LinkedIn" />](https://www.linkedin.com/in/alianblank)
[![Reddit](https://img.shields.io/badge/Reddit-FF4500?style=for-the-badge&logo=reddit&logoColor=white)](https://www.reddit.com/r/GameFrameX/)
[![X](https://img.shields.io/badge/X-000000?style=for-the-badge&logo=x&logoColor=white)](https://x.com/alian_blank)
[![YouTube](https://img.shields.io/badge/YouTube-FF0000?style=for-the-badge&logo=youtube&logoColor=white)](https://www.youtube.com/channel/UCD9QhSFJ5xZkn5NTSV-DVAw)
[![Bluesky](https://img.shields.io/badge/Bluesky-0285FF?style=for-the-badge&logo=bluesky&logoColor=white)](https://bsky.app/profile/alianblank.bsky.social)

## 更新日志

版本历史请参阅 [Releases](https://github.com/GameFrameX/GameFrameX.SuperSocket/releases)。

## 开源协议

详见 [LICENSE](LICENSE) 文件。

<!--
EN: See [LICENSE](LICENSE) for license information.
zh-CN: 详见 [LICENSE](LICENSE) 文件。
zh-TW: 詳見 [LICENSE](LICENSE) 檔案。
ja: 詳しくは [LICENSE](LICENSE) をご参照ください。
ko: 자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.
-->
