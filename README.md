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

**All-in-One Solution for Indie Game Development · Empowering Indie Developers' Dreams**

<br />

[Documentation](https://gameframex.doc.alianblank.com) · [Quick Start](#quick-start) · QQ Group: 467608841 / 233840761

<br />

**English** | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

</div>

## Project Overview

GameFrameX.SuperSocket is the GameFrameX maintained fork of [SuperSocket](https://github.com/kerryjiang/SuperSocket) — a light weight extensible socket application framework written in pure C#. You can use it to build an always connected socket application easily without thinking about how to use socket, how to maintain the socket connections and how socket works.

The upstream architecture and public APIs are preserved, so a project written against SuperSocket keeps working. On top of that this fork carries the changes GameFrameX game servers need: the .NET 10 build target, DI/constructor injection support, and the KCP / ReliableSession protocol adaptations for weak-network game traffic.

### Features

- Light weight and extensible — build always connected socket applications without managing sockets by hand.
- Pure C#, so it integrates into any existing .NET system.
- Protocol decoding pipeline with pipeline filters and package decoders.
- TCP is the default transport; UDP, KCP and ReliableSession are explicit opt-ins.
- KCP transport for reliable delivery over UDP datagrams, with retransmission and window control.
- ReliableSession protocol frame contract and binary codec for logical session resume, replay cursors, ack ranges, snapshot fallback and close/error frames.
- Command pattern request handling.
- WebSocket server and client, plus Kestrel integration.
- DI / constructor injection friendly host builder.
- .NET 10 build target.

## Quick Start

### Installation

Install the modules you need from NuGet.org:

```bash
dotnet add package GameFrameX.SuperSocket.Server
dotnet add package GameFrameX.SuperSocket.ProtoBase
```

The `Kcp` and `ReliableSession` modules come with the next release; until then, build them from a source checkout:

```bash
git clone https://github.com/GameFrameX/GameFrameX.SuperSocket.git
cd GameFrameX.SuperSocket
dotnet build GameFrameX.SuperSocket.slnx
```

Run the test suite from the same checkout:

```bash
dotnet test GameFrameX.SuperSocket.slnx
dotnet test test/GameFrameX.SuperSocket.ReliableSession.Tests/GameFrameX.SuperSocket.ReliableSession.Tests.csproj
```

## Usage Examples

### Transport Selection

TCP remains the default transport. UDP, KCP, and ReliableSession are explicit choices:

| Choice | Use when | Current behavior |
|:---|:---|:---|
| TCP | You want the standard SuperSocket connection path. | Default server/client transport. |
| Raw UDP | You want datagram delivery and can tolerate loss, duplication, and reordering yourself. | Explicit opt-in with `UseUdp()` / `AsUdp(...)`; unreliable datagram transport. |
| KCP | You want reliable delivery over UDP datagrams with KCP retransmission/window control. | Explicit opt-in with `UseKcp(...)` / `AsKcp(...)`; not KCP-over-TCP. |
| ReliableSession | You need a protocol contract for logical session resume, replay cursors, ack ranges, snapshot fallback, and close/error frames. | Protocol model and binary codec only. Runtime heartbeats, resume state, replay cache, dedup cache, adapters, and business delivery are not implemented in C3. |

### Server: Enable KCP

Reference `GameFrameX.SuperSocket.Kcp`, keep your normal package pipeline and handler, then add
`UseKcp(...)` to the host builder:

```csharp
using System.Text;
using GameFrameX.SuperSocket.Kcp;
using GameFrameX.SuperSocket.ProtoBase;
using GameFrameX.SuperSocket.Server.Host;

var builder = SuperSocketHostBuilder
    .Create<TextPackageInfo, LinePipelineFilter>()
    .UseKcp(options =>
    {
        // Unset nullable options keep KCP's internal defaults.
        options.NoDelay = true;
        options.NoDelayLevel = 1;
        options.Interval = 10;
        options.Resend = 2;
        options.NoCongestionControl = true;
        options.SendWindow = 512;
        options.ReceiveWindow = 512;
        options.MaxDatagramSize = 4096;

        // Raise this explicitly when you expect minute-level packet blackout.
        options.DeadLink = 120;
    })
    .UsePackageHandler(async (session, package) =>
    {
        // Handle the decoded SuperSocket package exactly as you do on TCP.
        await session.SendAsync(Encoding.UTF8.GetBytes(package.Text + "\r\n"));
    });
```

`UseKcp(...)` registers the KCP listener/factory and the default in-process session container when
one has not already been registered. The default KCP server session identity is built from the
remote endpoint plus the KCP `Conv` read from the incoming UDP packet. Endpoint/NAT migration is
therefore not supported by the KCP transport layer alone.

### Client: Use KCP

Reference `GameFrameX.SuperSocket.Kcp`, configure `EasyClient` with `AsKcp(...)`, and then use the
normal receive/send APIs on the client:

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
    // Conv = 0 lets the client generate a non-zero conversation id.
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

`AsKcp(...)` creates and binds a UDP socket, assigns or generates `Conv`, creates a `KcpPipeConnection`,
starts the KCP update loop, and starts receiving UDP packets for that connection. Set
`client.LocalEndPoint` before `AsKcp(...)` when the client must bind a specific local UDP endpoint.

### KCP Configuration Notes

- Leave nullable options unset unless you have a measured reason to tune them; unset values keep KCP
  internal defaults.
- For realtime game-style traffic, common starting points are `NoDelay = true`, `NoDelayLevel = 1`,
  `Interval = 10`, `Resend = 2`, and tuned send/receive windows.
- For conservative throughput, keep more defaults and avoid disabling congestion control.
- `DeadLink` is the maximum retransmission count for one KCP segment. The internal default is not a
  minute-level blackout policy; raise it deliberately when your acceptance condition requires longer
  blackout tolerance.
- `IdleTimeout` belongs to the connection lifetime layer. It is not a logical session recovery window.
- `MaxDatagramSize` should fit your network MTU strategy. Oversized UDP datagrams raise fragmentation
  and loss risk.

### ReliableSession Protocol Model

Reference `GameFrameX.SuperSocket.ReliableSession` when you need the protocol frame contract and
binary codec:

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

ReliableSession currently defines and validates these frame kinds: `Hello`, `HelloAck`, `Resume`,
`ResumeAck`, `Heartbeat`, `Data`, `Ack`, `SnapshotRequest`, `Snapshot`, `Close`, and `Error`.
The codec expects one complete ReliableSession frame per buffer; transport stream splitting/framing
belongs in a later adapter.

Current boundaries:

- No server/client runtime switch enables ReliableSession yet.
- No automatic heartbeat timer, reconnect loop, resume-token store, replay cache, dedup cache, or
  snapshot provider is included yet.
- KCP keeps using endpoint plus `Conv` as its transport session identity. ReliableSession's
  `SessionId` plus `ResumeToken` is the future logical-session resume contract, not current KCP
  endpoint migration support.
- C3 test coverage is protocol/codec end-to-end coverage, including lifecycle, 10s/30s/60s blackout
  resume scripts, replay, snapshot fallback, duplicate/reordered frames, and ack ranges. It is not
  runtime transport integration coverage.

## Architecture

The modules layer from protocol primitives up to the host:

- `Primitives` / `ProtoBase` — primitive interfaces and protocol decoding.
- `Connection` / `Channel` — the underlying communications abstraction and the request pipeline.
- `Server` / `Server.Abstractions` / `Client` / `ClientEngine` / `Client.Proxy` — host, server and client endpoints.
- `Command` — command pattern request handling on top of the server.
- `Udp` / `Kcp` / `ReliableSession` — the optional transports and the logical session protocol.
- `WebSocket` / `WebSocket.Server` / `Kestrel` / `Http` — HTTP-family protocols and hosting.

## Platform Support

- .NET 10.0
- Windows, macOS, Linux

## Dependencies

| Module | Package | Description |
|:---|:---|:---|
| Primitives | `GameFrameX.SuperSocket.Primitives` | Primitive interfaces and classes |
| ProtoBase | `GameFrameX.SuperSocket.ProtoBase` | Protocol decoding |
| Connection | `GameFrameX.SuperSocket.Connection` | Underlying communications abstraction with pipeline |
| Server Abstractions | `GameFrameX.SuperSocket.Server.Abstractions` | Server abstractions |
| Server | `GameFrameX.SuperSocket.Server` | Server host |
| Client | `GameFrameX.SuperSocket.Client` | Client endpoints |
| Client Engine | `GameFrameX.SuperSocket.ClientEngine` | Client engine |
| Client Proxy | `GameFrameX.SuperSocket.Client.Proxy` | Client proxy support |
| Command | `GameFrameX.SuperSocket.Command` | Command pattern request handling |
| Udp | `GameFrameX.SuperSocket.Udp` | UDP transport |
| Kcp | `GameFrameX.SuperSocket.Kcp` | KCP transport over UDP |
| ReliableSession | `GameFrameX.SuperSocket.ReliableSession` | ReliableSession protocol model and codec |
| WebSocket | `GameFrameX.SuperSocket.WebSocket` | WebSocket protocol implementation |
| WebSocket Server | `GameFrameX.SuperSocket.WebSocket.Server` | WebSocket server |
| Kestrel | `GameFrameX.SuperSocket.Kestrel` | Kestrel integration |
| Http | `GameFrameX.SuperSocket.Http` | Shared utilities for HTTP-like protocols |

Beyond the .NET base class library, the modules depend on `Microsoft.Extensions.*`
(Configuration, DependencyInjection, Hosting, Logging, Options), `System.IO.Pipelines`, and the
`Microsoft.AspNetCore.App` framework reference for the Kestrel module.

## Documentation & Resources

- [Documentation](https://gameframex.doc.alianblank.com)
- [GitHub Repository](https://github.com/GameFrameX/GameFrameX.SuperSocket)
- [Issue Tracker](https://github.com/GameFrameX/GameFrameX.SuperSocket/issues)
- [Upstream Project](https://github.com/kerryjiang/SuperSocket)
- [Upstream Documentation](https://docs.supersocket.net/)

## Community & Support

[![GitHub](https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/GameFrameX/gameframex)
[![Discord](https://img.shields.io/badge/Discord-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/VDWUjWMDw9)
[<img src="https://cdn.jsdelivr.net/npm/devicon@2/icons/linkedin/linkedin-original.svg" height="28" alt="LinkedIn" />](https://www.linkedin.com/in/alianblank)
[![Reddit](https://img.shields.io/badge/Reddit-FF4500?style=for-the-badge&logo=reddit&logoColor=white)](https://www.reddit.com/r/GameFrameX/)
[![X](https://img.shields.io/badge/X-000000?style=for-the-badge&logo=x&logoColor=white)](https://x.com/alian_blank)
[![YouTube](https://img.shields.io/badge/YouTube-FF0000?style=for-the-badge&logo=youtube&logoColor=white)](https://www.youtube.com/channel/UCD9QhSFJ5xZkn5NTSV-DVAw)
[![Bluesky](https://img.shields.io/badge/Bluesky-0285FF?style=for-the-badge&logo=bluesky&logoColor=white)](https://bsky.app/profile/alianblank.bsky.social)
[![Bilibili](https://img.shields.io/badge/Bilibili-00A1D6?style=for-the-badge&logo=bilibili&logoColor=white)](https://www.bilibili.com/video/BV1yrpeepEn7)
[![Gitee](https://img.shields.io/badge/Gitee-C71D23?style=for-the-badge&logo=gitee&logoColor=white)](https://gitee.com/GameFrameX/gameframex)
![QQ](https://img.shields.io/badge/QQ-467608841%2F233840761-EB1923?style=for-the-badge&logo=qq&logoColor=white)

## Changelog

See [Releases](https://github.com/GameFrameX/GameFrameX.SuperSocket/releases) for the version history.

## License

See [LICENSE](LICENSE) for license information.

<!--
EN: See [LICENSE](LICENSE) for license information.
zh-CN: 详见 [LICENSE](LICENSE) 文件。
zh-TW: 詳見 [LICENSE](LICENSE) 檔案。
ja: 詳しくは [LICENSE](LICENSE) をご参照ください。
ko: 자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.
-->
