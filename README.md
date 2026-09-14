# SuperSocket

[![Join the chat at https://gitter.im/supersocket/community](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/supersocket/community)
[![Build](https://github.com/kerryjiang/SuperSocket/workflows/build/badge.svg)](https://github.com/kerryjiang/SuperSocket/actions?query=workflow%3Abuild)
[![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.svg?style=flat)](https://www.nuget.org/packages/SuperSocket/)
[![NuGet](https://img.shields.io/nuget/dt/SuperSocket.svg)](https://www.nuget.org/packages/SuperSocket)
[![Badge](https://img.shields.io/badge/link-996.icu-red.svg)](https://996.icu/#/en_US)

**SuperSocket** is a light weight extensible socket application framework. You can use it to build an always connected socket application easily without thinking about how to use socket, how to maintain the socket connections and how socket works. It is a pure C# project which is designed to be
extended, so it is easy to be integrated to your existing systems as long as they are developed in .NET language.

- **Project homepage**:        [https://www.supersocket.net/](https://www.supersocket.net/)
- **Documentation**:        [https://docs.supersocket.net/](https://docs.supersocket.net/)
- **License**:                [https://www.apache.org/licenses/LICENSE-2.0](https://www.apache.org/licenses/LICENSE-2.0)

---

##### Nuget Packages

| Package                                                                               |                                                                                   MyGet Version                                                                                   |                                                                          NuGet Version                                                                          |                                                                            Download                                                                            |
|:--------------------------------------------------------------------------------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------:|:---------------------------------------------------------------------------------------------------------------------------------------------------------------:|:--------------------------------------------------------------------------------------------------------------------------------------------------------------:|
| **SuperSocket**  <br /> (all in one)                                                  |                  [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket)                  |                  [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.svg?style=flat)](https://www.nuget.org/packages/SuperSocket/)                  |                  [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.svg?style=flat)](https://www.nuget.org/packages/SuperSocket/)                  |
| ~~**SuperSocket.WebSocketServer**~~ <br /> (Use SuperSocket.WebSocket.Server instead) |  [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket.WebSocketServer)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket.WebSocketServer)  |  [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.WebSocketServer.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.WebSocketServer/)  |  [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.WebSocketServer.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.WebSocketServer/)  |
| **SuperSocket.ProtoBase**                                                             |        [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket.ProtoBase)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket.ProtoBase)        |        [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.ProtoBase.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.ProtoBase/)        |        [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.ProtoBase.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.ProtoBase/)        |
| **SuperSocket.Primitives**                                                            |       [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket.Primitives)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket.Primitives)       |       [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.Primitives.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Primitives/)       |       [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.Primitives.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Primitives/)       |
| ~~**SuperSocket.Channel**~~                                                           |          [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket.Channel)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket.Channel)          |          [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.Channel.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Channel/)          |          [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.Channel.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Channel/)          |
| **SuperSocket.Connection**                                                            |       [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket.Connection)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket.Connection)       |       [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.Connection.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Connection/)       |       [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.Connection.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Connection/)       |
| **SuperSocket.Server**                                                                |           [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket.Server)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket.Server)           |           [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.Server.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Server/)           |           [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.Server.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Server/)           |
| **SuperSocket.Command**                                                               |          [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket.Command)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket.Command)          |          [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.Command.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Command/)          |          [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.Command.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Command/)          |
| ~~**SuperSocket.SessionContainer**~~                                                  | [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket.SessionContainer)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket.SessionContainer) | [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.SessionContainer.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.SessionContainer/) | [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.SessionContainer.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.SessionContainer/) |
| **SuperSocket.Client**                                                                |           [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket.Client)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket.Client)           |           [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.Client.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Client/)           |           [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.Client.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Client/)           |
| **SuperSocket.Client.Proxy**                                                          |     [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket.Client.Proxy)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket.Client.Proxy)     |     [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.Client.Proxy.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Client.Proxy/)     |     [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.Client.Proxy.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Client.Proxy/)     |
| **SuperSocket.WebSocket**                                                             |        [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket.WebSocket)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket.WebSocket)        |        [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.WebSocket.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.WebSocket/)        |        [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.WebSocket.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.WebSocket/)        |
| **SuperSocket.WebSocket.Server**                                                      | [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket.WebSocket.Server)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket.WebSocket.Server) | [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.WebSocket.Server.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.WebSocket.Server/) | [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.WebSocket.Server.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.WebSocket.Server/) |
| **SuperSocket.Udp**                                                                   |              [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket.Udp)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket.Udp)              |              [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.Udp.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Udp/)              |              [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.Udp.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.Udp/)              |
| ~~**SuperSocket.GZip**~~                                                              |             [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket.GZip)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket.GZip)             |             [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.GZip.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.GZip/)             |             [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.GZip.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.GZip/)             |
| **SuperSocket.SerialIO**                                                              |         [![MyGet Version](https://img.shields.io/myget/supersocket/vpre/SuperSocket.SerialIO)](https://www.myget.org/feed/supersocket/package/nuget/SuperSocket.SerialIO)         |         [![NuGet Version](https://img.shields.io/nuget/vpre/SuperSocket.SerialIO.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.SerialIO/)         |         [![NuGet Download](https://img.shields.io/nuget/dt/SuperSocket.SerialIO.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.SerialIO/)         |

Nightly build packages:  https://www.myget.org/F/supersocket/api/v3/index.json

GameFrameX additions in this fork include `GameFrameX.SuperSocket.Kcp` for KCP transport and
`GameFrameX.SuperSocket.ReliableSession` for the ReliableSession protocol model and codec.

---

## Transport Selection

TCP remains the default transport. UDP, KCP, and ReliableSession are explicit choices:

| Choice | Use when | Current behavior |
|:---|:---|:---|
| TCP | You want the standard SuperSocket connection path. | Default server/client transport. |
| Raw UDP | You want datagram delivery and can tolerate loss, duplication, and reordering yourself. | Explicit opt-in with `UseUdp()` / `AsUdp(...)`; unreliable datagram transport. |
| KCP | You want reliable delivery over UDP datagrams with KCP retransmission/window control. | Explicit opt-in with `UseKcp(...)` / `AsKcp(...)`; not KCP-over-TCP. |
| ReliableSession | You need a protocol contract for logical session resume, replay cursors, ack ranges, snapshot fallback, and close/error frames. | Protocol model and binary codec only. Runtime heartbeats, resume state, replay cache, dedup cache, adapters, and business delivery are not implemented in C3. |

### Server: enable KCP

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

### Client: use KCP

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

### KCP configuration notes

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

### ReliableSession protocol model

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

Validation entry points:

```bash
dotnet test GameFrameX.SuperSocket.slnx
dotnet test test/GameFrameX.SuperSocket.ReliableSession.Tests/GameFrameX.SuperSocket.ReliableSession.Tests.csproj
```

---

## SuperSocket 2.0 Roadmap:

- 2024:
    - More documents
    - Performance test/tuning
    - Fix issues of the existing features
    - Other features requested by users
    - Stable release
