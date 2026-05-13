# GameFrameX.SuperSocket.Kcp — KCP 协议支持完整方案

> 项目：GameFrameX.SuperSocket
> 模块：GameFrameX.SuperSocket.Kcp（新增）
> 日期：2026-05-12
> 状态：implementing

---

## 目录

1. [变更意图（Why）](#1-变更意图why)
2. [背景与术语](#2-背景与术语)
3. [范围（What）](#3-范围what)
4. [架构设计](#4-架构设计)
5. [模块结构](#5-模块结构)
6. [核心类详细设计](#6-核心类详细设计)
7. [KCP 核心移植方案](#7-kcp-核心移植方案)
8. [数据流设计](#8-数据流设计)
9. [会话管理与路由](#9-会话管理与路由)
10. [配置与参数](#10-配置与参数)
11. [实施方案（TODO）](#11-实施方案todo)
12. [验收标准（AC）](#12-验收标准ac)
13. [测试方案](#13-测试方案)
14. [影响评估](#14-影响评估)
15. [风险与缓解](#15-风险与缓解)
16. [开放问题](#16-开放问题)
17. [参考资源](#17-参考资源)
18. [附录](#18-附录)

---

## 1. 变更意图（Why）

### 1.1 问题

当前 GameFrameX.SuperSocket 支持 TCP 和 UDP 两种传输层协议：

- **TCP**：可靠有序，但延迟较高（拥塞控制、重传等待），不适合实时游戏
- **UDP**：低延迟，但不可靠（丢包、乱序、无拥塞控制），需要上层自行处理

游戏服务器场景（如实时对战、帧同步、状态同步）需要一种**低延迟 + 可靠**的传输方案，TCP 和纯 UDP 都不能完美满足需求。

### 1.2 解决方案

引入 **KCP 协议**（Quick Reliable Protocol）。KCP 是一个基于 UDP 的 ARQ（自动重传请求）协议，核心优势：

| 特性 | TCP | KCP | UDP |
|------|-----|-----|-----|
| 可靠性 | ✅ | ✅ | ❌ |
| 有序性 | ✅ | ✅ | ❌ |
| 平均延迟 | 高 | 低 | 最低 |
| 丢包时延迟恶化 | 严重（拥塞控制） | 温和（快速重传） | N/A |
| 适合实时游戏 | ❌ | ✅ | 部分场景 |

### 1.3 目标

新增 `GameFrameX.SuperSocket.Kcp` 模块，提供与现有 `UseUdp()` / TCP 完全一致的使用体验：

```csharp
var host = MultipleServerHostBuilder.Create()
    .AddServer<MyPackage>(builder => builder
        .UseKcp()                          // 一行切换
        .UsePipelineFilter<MyFilter>()
        .ConfigureOptions(opt => {
            opt.AddListener(new ListenOptions { Ip = "0.0.0.0", Port = 8000 });
        })
    );
```

---

## 2. 背景与术语

| 术语 | 含义 |
|------|------|
| **KCP** | Quick Reliable Protocol，由 Skywind3000 设计的低延迟可靠传输协议 |
| **Conv** | Conversation ID，KCP 会话标识（uint32），用于区分不同连接 |
| **Token** | KCP 握手令牌（uint32），用于连接鉴权 |
| **MTU** | Maximum Transmission Unit，最大传输单元 |
| **ARQ** | Automatic Repeat reQuest，自动重传请求 |
| **RTO** | Retransmission Timeout，重传超时时间 |
| **SND_UNA** | 发送未确认序号 |
| **SND_NXT** | 下一个发送序号 |
| **RCV_NXT** | 下一个接收序号 |
| **VirtualConnection** | SuperSocket 中非独占 Socket 的连接抽象（UDP/KCP 共用 Socket） |
| **Pipe** | System.IO.Pipelines，高性能内存管道 |

---

## 3. 范围（What）

### 3.1 In Scope（本次做）

- ✅ KCP 核心协议的 C# 移植（基于 ikcp.c）
- ✅ 新增 `GameFrameX.SuperSocket.Kcp` 项目
- ✅ `KcpPipeConnection` — KCP 连接实现
- ✅ `KcpConnectionListener` — KCP 监听器（UDP Socket + Conv 路由）
- ✅ `KcpServerHostBuilderExtensions.UseKcp()` — DI 注册与扩展方法
- ✅ 基于 Conv 的会话标识路由（默认实现）
- ✅ KCP 参数可配置（nodelay、窗口、MTU 等）
- ✅ 连接超时与空闲回收
- ✅ 解决方案文件集成（.sln）
- ✅ XML 文档注释

### 3.2 Out of Scope（本次不做）

- ❌ KCP 客户端封装（后续可加 `GameFrameX.SuperSocket.Kcp.Client`）
- ❌ KCP 加密/ FEC（前向纠错）
- ❌ KCP over WebSocket
- ❌ KCP 握手协议（首次 Conv 分配由上层通过 `IKcpSessionIdentifierProvider` 自定义）
- ❌ 性能压测与调优（后续独立任务）
- ❌ 连接迁移（网络切换后恢复 KCP 会话）

### 3.3 涉及文件（预估新增）

```
src/GameFrameX.SuperSocket.Kcp/
├── GameFrameX.SuperSocket.Kcp.csproj
├── Kcp/KcpCore.cs                          // KCP 核心移植
├── Kcp/KcpSegment.cs                       // KCP 数据段
├── Kcp/KcpSegmentManager.cs                // 段池化管理
├── Kcp/KcpConstants.cs                     // 常量定义
├── KcpConnectionOptions.cs                 // KCP 配置选项
├── KcpConnectionInfo.cs                    // 连接信息
├── KcpPipeConnection.cs                    // KCP 连接
├── KcpConnectionListener.cs               // KCP 监听器
├── KcpConnectionListenerFactory.cs        // 监听器工厂
├── KcpConnectionFactory.cs               // 连接工厂
├── KcpConnectionFactoryBuilder.cs         // 连接工厂构建器
├── IKcpSessionIdentifierProvider.cs       // 会话标识接口
├── KcpConvIdentifierProvider.cs           // 默认 Conv 路由实现
└── KcpServerHostBuilderExtensions.cs      // UseKcp() 扩展
```

修改的现有文件：
```
GameFrameX.SuperSocket.sln                    // 添加新项目引用
src/src.sln                                    // 添加新项目引用
```

---

## 4. 架构设计

### 4.1 模块定位

```
┌──────────────────────────────────────────────────────────┐
│                    用户应用层                              │
│  (PackageHandler, Command, Session 等)                   │
├──────────────────────────────────────────────────────────┤
│              SuperSocket Server 抽象层                    │
│  (ISuperSocketHostBuilder, IConnectionListener, etc.)    │
├──────────┬──────────┬──────────────┬─────────────────────┤
│   TCP    │   UDP    │    KCP (新)   │   WebSocket         │
│ .Server  │   .Udp   │    .Kcp      │  .WebSocket.Server  │
├──────────┴──────────┴──────────────┴─────────────────────┤
│              Connection 层                                │
│  (PipeConnectionBase, VirtualConnection, Pipe)           │
├──────────────────────────────────────────────────────────┤
│              ProtoBase 层                                 │
│  (IPipelineFilter, IPackageEncoder, etc.)                │
└──────────────────────────────────────────────────────────┘
```

### 4.2 继承关系

```
ConnectionBase
  └── PipeConnectionBase (PipeReader/PipeWriter + PipelineFilter 驱动)
        └── PipeConnection (Input Pipe + Output Pipe 双管道)
              └── VirtualConnection (外部写入 Input Pipe，不从 Socket 读)
                    ├── UdpPipeConnection    (现有)
                    └── KcpPipeConnection    (新增)
```

### 4.3 与 Udp 模块的关系

KCP 模块**不依赖** Udp 模块，两者平行：

```
GameFrameX.SuperSocket.Kcp
  ├── 依赖: GameFrameX.SuperSocket.Primitives
  ├── 依赖: GameFrameX.SuperSocket.Connection
  └── 依赖: GameFrameX.SuperSocket.Server.Abstractions
```

两者共享底层 Connection 抽象（`VirtualConnection`、`UdpPipeConnection` 中类似的结构），但各自独立实现监听和连接逻辑。

### 4.4 数据流概览

```
接收方向 (Inbound):
  Socket.ReceiveFromAsync()
    → KcpConnectionListener (按 Conv 路由)
      → KcpPipeConnection.KcpInput(rawUdpData)
        → KcpCore.Input() (KCP 协议处理)
          → 完整消息 → VirtualConnection.WriteInputPipeDataAsync()
            → Input Pipe → PipelineFilter → PackageHandler

发送方向 (Outbound):
  Session.SendAsync(package)
    → PipelineFilter.Encode()
      → Output Pipe
        → KcpPipeConnection.SendOverIOAsync()
          → KcpCore.Send(data)
            → KCP 缓冲区（分片 + 编号）
              → KcpCore.Update() 周期性产出待发送 UDP 包
                → Socket.SendToAsync()
```

---

## 5. 模块结构

### 5.1 目录布局

```
src/GameFrameX.SuperSocket.Kcp/
│
├── Kcp/                                    # KCP 协议核心（移植层）
│   ├── KcpConstants.cs                     # 协议常量
│   ├── KcpSegment.cs                       # 数据段（替代 ikcp_segment）
│   ├── KcpSegmentManager.cs               # 段对象池
│   └── KcpCore.cs                          # KCP 核心（替代 ikcp.c）
│
├── KcpConnectionOptions.cs                # KCP 连接配置
├── KcpConnectionInfo.cs                    # 连接创建信息
├── KcpPipeConnection.cs                    # KCP 虚拟连接
├── KcpConnectionListener.cs               # KCP 监听器
├── KcpConnectionListenerFactory.cs        # 监听器工厂
├── KcpConnectionFactory.cs               # 连接工厂
├── KcpConnectionFactoryBuilder.cs         # 连接工厂构建器
├── IKcpSessionIdentifierProvider.cs       # 会话标识接口
├── KcpConvIdentifierProvider.cs           # 默认实现（基于 Conv）
└── KcpServerHostBuilderExtensions.cs      # UseKcp() 扩展方法
```

### 5.2 项目文件（csproj）

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Description>
      SuperSocket KCP protocol support library.
      GameFrameX 框架的 KCP 协议支持库。
      框架文档主页: https://gameframex.doc.alianblank.com
    </Description>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\GameFrameX.SuperSocket.Primitives\GameFrameX.SuperSocket.Primitives.csproj"/>
    <ProjectReference Include="..\GameFrameX.SuperSocket.Connection\GameFrameX.SuperSocket.Connection.csproj"/>
    <ProjectReference Include="..\GameFrameX.SuperSocket.Server.Abstractions\GameFrameX.SuperSocket.Server.Abstractions.csproj"/>
  </ItemGroup>
</Project>
```

---

## 6. 核心类详细设计

### 6.1 KcpCore（KCP 核心协议）

```csharp
namespace GameFrameX.SuperSocket.Kcp.Kcp
{
    /// <summary>
    /// KCP 协议核心实现（移植自 ikcp.c）。
    /// 提供可靠的 ARQ 传输能力，运行在 UDP 之上。
    /// </summary>
    internal class KcpCore
    {
        // === 连接状态 ===
        private uint _conv;           // 会话 ID
        private uint _mtu;            // 最大传输单元
        private uint _mss;            // 最大段大小 (mtu - header)
        private uint _state;          // 连接状态 (0=可用, -1=已关闭)

        // === 发送相关 ===
        private uint _snd_una;        // 未确认的发送序号
        private uint _snd_nxt;        // 下一个发送序号
        private uint _rcv_nxt;        // 下一个期望接收序号
        private uint _cwnd;           // 拥塞窗口
        private uint _rmt_wnd;        // 远端窗口大小
        private uint _cwnd;           // 拥塞窗口

        // === RTT 估计 ===
        private int32 _rx_rttval;     // RTT 方差
        private int32 _rx_srtt;       // 平滑 RTT
        private int32 _rx_rto;        // 重传超时
        private int32 _rx_minrto;     // 最小 RTO

        // === 数据结构 ===
        private readonly Queue<KcpSegment> _snd_queue;   // 发送队列
        private readonly Queue<KcpSegment> _rcv_queue;   // 接收队列
        private readonly LinkedList<KcpSegment> _snd_buf; // 发送缓冲
        private readonly LinkedList<KcpSegment> _rcv_buf; // 接收缓冲
        private readonly KcpSegmentManager _segmentManager;

        // === 回调 ===
        /// <summary>
        /// KCP 需要发送 UDP 包时的回调。参数为待发送的原始字节。
        /// </summary>
        public Action<Memory<byte>> Output { get; set; }

        // === 核心方法 ===

        /// <summary>处理收到的 UDP 原始数据</summary>
        public int Input(ReadOnlySpan<byte> data);

        /// <summary>发送应用层数据（经 KCP 分片、编号后入缓冲）</summary>
        public int Send(ReadOnlySpan<byte> data);

        /// <summary>接收 KCP 重组后的完整消息</summary>
        public int Recv(Span<byte> buffer);

        /// <summary>驱动 KCP 状态机（检查重传、发送 ACK 等），返回下次 Update 时间</summary>
        public uint Update(uint current);

        /// <summary>检查是否有完整消息可读</summary>
        public bool PeekCanRecv();

        /// <summary>获取等待发送的数据大小</summary>
        public int WaitSnd();

        // === 配置方法 ===
        public void SetMtu(uint mtu);
        public void SetWindowSize(int sendWindow, int receiveWindow);
        public void SetNoDelay(int nodelay, int interval, int resend, int nc);
        public void SetNormalMode();
        public void SetFastMode();
    }
}
```

#### KCP 头部格式（24 字节）

```
Offset  Size  Field
0       4     conv      (会话 ID)
4       1     cmd       (命令类型: 81=DATA, 82=ACK, 83=WASK, 84=WINS)
5       1     frg       (分片编号, 0=最后一个分片)
6       2     wnd       (可用窗口大小)
8       4     ts        (时间戳)
12      4     sn        (序号)
16      4     una       (未确认序号)
20      4     len       (数据长度)
---
Total: 24 bytes header
```

### 6.2 KcpSegment

```csharp
namespace GameFrameX.SuperSocket.Kcp.Kcp
{
    /// <summary>
    /// KCP 数据段，对应 ikcp_segment。
    /// 使用对象池管理以减少 GC 压力。
    /// </summary>
    internal class KcpSegment
    {
        public uint Conv;
        public byte Cmd;
        public byte Frg;
        public ushort Wnd;
        public uint Ts;
        public uint Sn;
        public uint Una;
        public uint Resendts;      // 重发时间戳
        public uint Rto;           // 重传超时
        public uint Fastack;       // 快速 ACK 计数
        public uint Xmit;          // 已发送次数
        public byte[] Data;        // 数据缓冲区
        public int DataLength;     // 数据有效长度

        // 链表节点支持（用于 snd_buf / rcv_buf）
        public KcpSegment Next;
        public KcpSegment Prev;
    }
}
```

### 6.3 KcpSegmentManager

```csharp
namespace GameFrameX.SuperSocket.Kcp.Kcp
{
    /// <summary>
    /// KCP 数据段对象池。
    /// 避免频繁 new/GC，提升高频收发场景性能。
    /// </summary>
    internal class KcpSegmentManager
    {
        private readonly ConcurrentBag<KcpSegment> _pool;
        private readonly int _maxPoolSize;

        public KcpSegmentManager(int maxPoolSize = 1024);
        public KcpSegment Rent(uint dataSize);
        public void Return(KcpSegment segment);
    }
}
```

### 6.4 KcpPipeConnection（核心连接）

```csharp
namespace GameFrameX.SuperSocket.Kcp
{
    /// <summary>
    /// 基于 KCP 协议的虚拟连接。
    /// 继承 VirtualConnection，复用 SuperSocket 的 Pipe + PipelineFilter 体系。
    /// </summary>
    public class KcpPipeConnection : VirtualConnection, IConnectionWithSessionIdentifier
    {
        private readonly Socket _socket;
        private readonly IPEndPoint _remoteEndPoint;
        private readonly KcpCore _kcp;
        private readonly KcpConnectionOptions _kcpOptions;
        private readonly PeriodicTimer _updateTimer;
        private readonly CancellationTokenSource _cts;

        /// <summary>Conv 会话标识</summary>
        public uint Conv { get; }

        /// <summary>Session 标识（由 IKcpSessionIdentifierProvider 生成）</summary>
        public string SessionIdentifier { get; }

        // === 构造函数 ===
        public KcpPipeConnection(
            Socket socket,
            IPEndPoint remoteEndPoint,
            string sessionIdentifier,
            ConnectionOptions options,
            KcpConnectionOptions kcpOptions);

        // === 对外接口（供 Listener 调用）===

        /// <summary>
        /// 接收 UDP 原始数据，送入 KCP 协议栈处理。
        /// 由 KcpConnectionListener 在收到 UDP 包时调用。
        /// </summary>
        public void InputUdpPacket(ReadOnlyMemory<byte> data);

        // === 重写 VirtualConnection / PipeConnection 的方法 ===

        protected override void Close();
        protected override ValueTask<int> SendOverIOAsync(
            ReadOnlySequence<byte> buffer, CancellationToken cancellationToken);
        protected override Task ProcessSends();

        // === KCP 特有 ===

        /// <summary>KCP Update 循环（定时器驱动）</summary>
        private async Task UpdateLoopAsync();

        /// <summary>KCP 需要发送 UDP 包时的回调</summary>
        private void OnKcpOutput(Memory<byte> data);
    }
}
```

#### 关键行为说明

| 方法 | 行为 |
|------|------|
| `InputUdpPacket(data)` | 调用 `_kcp.Input(data)` → 检查 `_kcp.PeekCanRecv()` → 循环 `_kcp.Recv()` 取出完整消息 → `WriteInputPipeDataAsync()` 写入上层 Pipe |
| `SendOverIOAsync(buffer)` | 将 buffer 拷贝后调用 `_kcp.Send(data)`，数据进入 KCP 发送缓冲。**不直接发 UDP 包** |
| `OnKcpOutput(data)` | `_kcp.Update()` 产生的待发送 UDP 包 → `_socket.SendToAsync(data, RemoteEndPoint)` |
| `UpdateLoopAsync` | 每 10ms（可配置）调用 `_kcp.Update(currentMs)`，驱动重传/ACK/窗口更新 |
| `Close()` | 停止 Update 定时器，清理 KCP 资源，完成 Input Pipe Writer |

### 6.5 KcpConnectionListener

```csharp
namespace GameFrameX.SuperSocket.Kcp
{
    /// <summary>
    /// KCP 连接监听器。
    /// 在 UDP Socket 上监听，按 Conv 标识路由到不同的 KcpPipeConnection。
    /// </summary>
    class KcpConnectionListener : IConnectionListener
    {
        private Socket _listenSocket;
        private IPEndPoint _acceptRemoteEndPoint;
        private readonly IKcpSessionIdentifierProvider _identifierProvider;
        private readonly IAsyncSessionContainer _sessionContainer;

        // 核心接收循环
        private async Task KeepAccept(Socket listenSocket)
        {
            while (!_cts.IsCancellationRequested)
            {
                var result = await listenSocket.ReceiveFromAsync(buffer, ...);
                var remoteEndPoint = result.RemoteEndPoint;
                var packageData = buffer[..result.ReceivedBytes];

                // 1. 从 UDP 包提取 Conv + 生成 SessionID
                var sessionId = _identifierProvider.GetSessionIdentifier(remoteEndPoint, packageData);

                // 2. 查找已有 Session
                var session = await _sessionContainer.GetSessionByIDAsync(sessionId);

                if (session != null)
                {
                    // 3a. 已有连接 → 直接投递 UDP 数据
                    var kcpConn = session.Connection as KcpPipeConnection;
                    kcpConn.InputUdpPacket(packageData);
                }
                else
                {
                    // 3b. 新连接 → 创建 KcpPipeConnection → 触发 OnNewConnectionAccept
                    var connection = await CreateConnection(listenSocket, remoteEndPoint, sessionId);
                    OnNewConnectionAccept(connection);
                    // 首包也要投递
                    (connection as KcpPipeConnection).InputUdpPacket(packageData);
                }
            }
        }
    }
}
```

**与 UdpConnectionListener 的关键区别**：
- Udp 使用 `WriteInputPipeDataAsync()` 将原始数据写入 Pipe
- KCP 使用 `InputUdpPacket()` 将原始数据送入 KCP 协议栈，由 KCP 重组后再写入 Pipe

### 6.6 IKcpSessionIdentifierProvider

```csharp
namespace GameFrameX.SuperSocket.Kcp
{
    /// <summary>
    /// KCP 会话标识提供者。
    /// 从 UDP 包中提取标识信息（如 Conv），用于路由到正确的连接。
    /// </summary>
    public interface IKcpSessionIdentifierProvider
    {
        /// <summary>
        /// 从收到的 UDP 包中提取会话标识。
        /// </summary>
        /// <param name="remoteEndPoint">远端地址</param>
        /// <param name="data">UDP 包原始数据</param>
        /// <returns>会话唯一标识</returns>
        string GetSessionIdentifier(IPEndPoint remoteEndPoint, ReadOnlySpan<byte> data);
    }
}
```

### 6.7 KcpConvIdentifierProvider（默认实现）

```csharp
namespace GameFrameX.SuperSocket.Kcp
{
    /// <summary>
    /// 基于 KCP Conv 的默认会话标识提供者。
    /// 从 UDP 包前 4 字节读取 Conv，拼接 IP:Port:Conv 作为唯一标识。
    /// </summary>
    class KcpConvIdentifierProvider : IKcpSessionIdentifierProvider
    {
        public string GetSessionIdentifier(IPEndPoint remoteEndPoint, ReadOnlySpan<byte> data)
        {
            if (data.Length < 4)
                throw new ProtocolException("KCP packet too short to extract Conv");

            uint conv = BinaryPrimitives.ReadUInt32LittleEndian(data);
            return $"{remoteEndPoint.Address}:{remoteEndPoint.Port}:{conv}";
        }
    }
}
```

### 6.8 KcpConnectionOptions

```csharp
namespace GameFrameX.SuperSocket.Kcp
{
    /// <summary>
    /// KCP 连接配置选项。
    /// </summary>
    public class KcpConnectionOptions
    {
        /// <summary>会话 ID（Conv）。0 表示由服务端自动分配。</summary>
        public uint Conv { get; set; }

        /// <summary>最大传输单元。默认 1400（避免 IP 分片）。</summary>
        public uint Mtu { get; set; } = 1400;

        /// <summary>发送窗口大小。默认 256。</summary>
        public int SendWindow { get; set; } = 256;

        /// <summary>接收窗口大小。默认 256。</summary>
        public int ReceiveWindow { get; set; } = 256;

        /// <summary>
        /// 是否启用 NoDelay 模式。
        /// true: 快速重传，适合实时游戏。
        /// false: 普通模式，流量利用更高。
        /// </summary>
        public bool NoDelay { get; set; } = true;

        /// <summary>
        /// NoDelay 模式下的内部参数。
        /// nodelay=0: 关闭; nodelay=1: 开启快速重传; nodelay=2: 极速模式。
        /// </summary>
        public int NoDelayLevel { get; set; } = 1;

        /// <summary>Update 间隔（毫秒）。默认 10。</summary>
        public int Interval { get; set; } = 10;

        /// <summary>快速重传阈值。0 表示关闭。默认 0。</summary>
        public int Resend { get; set; } = 0;

        /// <summary>是否关闭拥塞控制。true=关闭。默认 true。</summary>
        public bool NoCongestionControl { get; set; } = true;

        /// <summary>连接空闲超时（秒）。默认 120。</summary>
        public int IdleTimeout { get; set; } = 120;

        /// <summary>段对象池最大大小。默认 1024。</summary>
        public int SegmentPoolSize { get; set; } = 1024;
    }
}
```

### 6.9 KcpConnectionInfo

```csharp
namespace GameFrameX.SuperSocket.Kcp
{
    /// <summary>
    /// KCP 连接创建所需信息。
    /// </summary>
    internal struct KcpConnectionInfo
    {
        public Socket Socket { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }
        public string SessionIdentifier { get; set; }
        public ConnectionOptions ConnectionOptions { get; set; }
        public KcpConnectionOptions KcpOptions { get; set; }
    }
}
```

### 6.10 KcpServerHostBuilderExtensions

```csharp
namespace GameFrameX.SuperSocket.Kcp
{
    public static class KcpServerHostBuilderExtensions
    {
        /// <summary>
        /// 配置 HostBuilder 使用 KCP 协议（默认参数）。
        /// </summary>
        public static ISuperSocketHostBuilder UseKcp(this ISuperSocketHostBuilder hostBuilder)
        {
            return hostBuilder.UseKcp(_ => { });
        }

        /// <summary>
        /// 配置 HostBuilder 使用 KCP 协议（自定义参数）。
        /// </summary>
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
                    services.TryAddEnumerable(ServiceDescriptor.Singleton<IMiddleware,
                        InProcSessionContainerMiddleware>(...));
                    services.AddSingleton<InProcSessionContainerMiddleware>();
                    services.AddSingleton<ISessionContainer>(...);
                    services.AddSingleton<IAsyncSessionContainer>(...);
                }
            });
        }

        public static ISuperSocketHostBuilder<TReceivePackage> UseKcp<TReceivePackage>(
            this ISuperSocketHostBuilder<TReceivePackage> hostBuilder)
        {
            return (hostBuilder as ISuperSocketHostBuilder).UseKcp() as ISuperSocketHostBuilder<TReceivePackage>;
        }

        public static ISuperSocketHostBuilder<TReceivePackage> UseKcp<TReceivePackage>(
            this ISuperSocketHostBuilder<TReceivePackage> hostBuilder,
            Action<KcpConnectionOptions> configure)
        {
            return (hostBuilder as ISuperSocketHostBuilder).UseKcp(configure)
                as ISuperSocketHostBuilder<TReceivePackage>;
        }
    }
}
```

---

## 7. KCP 核心移植方案

### 7.1 移植策略

基于 [ikcp.c](https://github.com/skywind3000/kcp/blob/master/ikcp.c) 逐函数移植为 C#，不引入任何外部依赖。

| ikcp.c 函数 | C# 对应方法 | 说明 |
|-------------|-------------|------|
| `ikcp_create` | `KcpCore` 构造函数 | 初始化 KCP 实例 |
| `ikcp_release` | `KcpCore.Dispose` | 释放资源 |
| `ikcp_recv` | `KcpCore.Recv` | 接收重组后的完整数据 |
| `ikcp_send` | `KcpCore.Send` | 发送数据（分片入队） |
| `ikcp_update` | `KcpCore.Update` | 状态机驱动 |
| `ikcp_check` | `KcpCore.Check` | 检查下次 Update 时间 |
| `ikcp_input` | `KcpCore.Input` | 处理收到的 UDP 包 |
| `ikcp_nodelay` | `KcpCore.SetNoDelay` | 设置 NoDelay 参数 |
| `ikcp_wndsize` | `KcpCore.SetWindowSize` | 设置窗口大小 |
| `ikcp_mtu` | `KcpCore.SetMtu` | 设置 MTU |
| `ikcp_flush` | `KcpCore.Flush` | 内部：刷新发送缓冲 |
| `ikcp_parse_ack` | `KcpCore.ParseAck` | 内部：处理 ACK |
| `ikcp_parse_una` | `KcpCore.ParseUna` | 内部：处理 UNA |
| `ikcp_parse_data` | `KcpCore.ParseData` | 内部：处理数据段 |
| `ikcp_parse_fastack` | `KcpCore.ParseFastack` | 内部：快速 ACK |
| `ikcp_wnd_unused` | `KcpCore.WndUnused` | 内部：可用窗口 |

### 7.2 移植注意事项

| 要点 | 说明 |
|------|------|
| **内存管理** | ikcp.c 大量 `malloc/free` → C# 用 `ArrayPool<byte>` + `KcpSegmentManager` 对象池 |
| **链表** | ikcp.c 用手写双向链表 `iqueue` → C# 用 `LinkedList<KcpSegment>` 或手写链（避免 LinkedList 节点 GC） |
| **字节序** | ikcp.c 用 `ikcp_encode32u` / `ikcp_decode32u` → C# 用 `BinaryPrimitives.WriteUInt32LittleEndian` |
| **时间** | ikcp.c 用毫秒时间戳 → C# 用 `Environment.TickCount64` / `Stopwatch.GetTimestamp()` |
| **指针操作** | ikcp.c 大量指针算术 → C# 用 `Span<byte>` + `MemoryMarshal` |
| **回调** | ikcp.c 用函数指针 `output` → C# 用 `Action<Memory<byte>>` 委托 |

### 7.3 移植规模估计

| 文件 | 行数（估计） |
|------|-------------|
| `KcpCore.cs` | ~1200 行 |
| `KcpSegment.cs` | ~50 行 |
| `KcpSegmentManager.cs` | ~60 行 |
| `KcpConstants.cs` | ~30 行 |
| **合计** | **~1340 行** |

---

## 8. 数据流设计

### 8.1 接收数据流（详细）

```
                         UDP Socket
                             │
                    ReceiveFromAsync()
                             │
                    ┌────────▼────────┐
                    │  KcpConnection   │
                    │    Listener      │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              │ 查找 Session │              │
              │ (按 Conv+IP) │              │
              └──────┬───────┘              │
                     │                      │
          ┌──────────▼──────────┐           │ 新连接
          │  已有连接            │           │
          │  KcpPipeConnection  │    ┌──────▼──────────┐
          │  .InputUdpPacket()  │    │ CreateConnection │
          └──────────┬──────────┘    └──────┬──────────┘
                     │                      │
              ┌──────▼──────┐        ┌──────▼──────┐
              │  KcpCore    │        │  new Kcp    │
              │  .Input()   │        │  Pipe       │
              └──────┬──────┘        │  Connection │
                     │               └──────┬──────┘
              ┌──────▼──────┐               │
              │ 解析 KCP 头  │               │
              │ ACK/数据/窗口 │               │
              └──────┬──────┘               │
                     │                      │
              ┌──────▼──────┐               │
              │ 重组 & 排序  │               │
              │ (rcv_buf →  │               │
              │  rcv_queue) │               │
              └──────┬──────┘               │
                     │                      │
              ┌──────▼──────┐               │
              │ KcpCore     │               │
              │ .Recv()     │               │
              │ 取出完整消息 │               │
              └──────┬──────┘               │
                     │                      │
              ┌──────▼──────────────────────▼──┐
              │  VirtualConnection              │
              │  .WriteInputPipeDataAsync()     │
              └──────┬──────────────────────────┘
                     │
              ┌──────▼──────┐
              │  Input Pipe  │
              │  (Reader)    │
              └──────┬──────┘
                     │
              ┌──────▼──────┐
              │ Pipeline    │
              │ Filter      │
              └──────┬──────┘
                     │
              ┌──────▼──────┐
              │  Package    │
              │  Handler    │
              └─────────────┘
```

### 8.2 发送数据流（详细）

```
  PackageHandler / Session.SendAsync()
                     │
              ┌──────▼──────┐
              │  Package    │
              │  Encoder    │
              └──────┬──────┘
                     │
              ┌──────▼──────┐
              │ Output Pipe │
              │ (Writer)    │
              └──────┬──────┘
                     │
              ┌──────▼──────────────┐
              │ KcpPipeConnection   │
              │ .SendOverIOAsync()  │
              └──────┬──────────────┘
                     │
              ┌──────▼──────┐
              │  KcpCore    │
              │  .Send()    │  ← 分片 + 编号，入 snd_queue
              └──────┬──────┘
                     │
              ┌──────▼──────┐
              │  snd_queue  │  ← 等待发送的段
              └──────┬──────┘
                     │ (由 Update 驱动)
              ┌──────▼──────┐
              │  KcpCore    │
              │  .Update()  │  ← 每 10ms 调用
              │  .Flush()   │  ← 滑窗口、重传、ACK
              └──────┬──────┘
                     │
              ┌──────▼──────┐
              │ OnKcpOutput │  ← 回调：有 UDP 包待发送
              └──────┬──────┘
                     │
              ┌──────▼──────┐
              │ Socket      │
              │ .SendToAsync│
              └──────┬──────┘
                     │
                  Network
```

### 8.3 Update 定时器流

```
  PeriodicTimer (10ms)
        │
  ┌─────▼─────┐
  │ UpdateLoop │ ← 每个 KcpPipeConnection 独立运行
  │ Async()    │
  └─────┬─────┘
        │
  ┌─────▼──────────┐
  │ current = Now  │
  │ _kcp.Update(   │
  │   current)     │
  └─────┬──────────┘
        │
  ┌─────▼──────────────┐
  │ 内部处理:           │
  │ 1. 检查超时重传     │
  │ 2. 发送 ACK        │
  │ 3. 窗口探测        │
  │ 4. 拥塞控制更新    │
  │ 5. Flush 发送缓冲  │
  └─────┬──────────────┘
        │
  ┌─────▼──────────┐
  │ OnKcpOutput()  │ ← 可能触发 0~N 次
  │ Socket.SendTo  │
  └────────────────┘
```

---

## 9. 会话管理与路由

### 9.1 会话标识策略

KCP 的会话路由依赖 **Conv**（4 字节 uint32），这是一个与 UDP 的 IP:Port 标识的关键差异：

| 方面 | UDP | KCP |
|------|-----|-----|
| 标识来源 | IP + Port | IP + Port + **Conv** |
| 同一 IP:Port | 只能一个连接 | 可多个连接（不同 Conv） |
| NAT 穿透后 | IP:Port 可能变 | Conv 不变，可保持连接 |
| 客户端分配 | 无需 | 需要预分配或握手协商 |

### 9.2 默认路由（KcpConvIdentifierProvider）

```
SessionID = "{IP}:{Port}:{Conv}"
```

从每个 UDP 包的前 4 字节（KCP 头的 conv 字段）读取 Conv，拼接 IP:Port 作为唯一标识。

### 9.3 自定义路由

用户可实现 `IKcpSessionIdentifierProvider`，例如：
- 纯 Conv 路由（不绑定 IP:Port，支持 NAT 切换）
- Token 鉴权路由（在 KCP 头之前附加 Token）
- 业务 ID 路由（按角色 ID / 房间 ID 路由）

### 9.4 Conv 分配策略

Conv 由**客户端选择**或**握手协商**，默认策略下：

- **简单模式**：客户端随机生成 Conv，服务端被动接受
- **安全模式**（建议后续扩展）：服务端通过独立 TCP/HTTP 接口分配 Conv + Token

---

## 10. 配置与参数

### 10.1 KCP 模式预设

```csharp
// 默认模式：平衡延迟和带宽
builder.UseKcp();

// 快速模式（游戏推荐）：最低延迟
builder.UseKcp(options => {
    options.NoDelay = true;
    options.NoDelayLevel = 2;      // 极速模式
    options.Interval = 10;         // 10ms Update
    options.Resend = 2;            // 快速重传阈值
    options.NoCongestionControl = true;
});

// 普通模式：流量优先
builder.UseKcp(options => {
    options.NoDelay = false;
    options.Interval = 40;
    options.NoCongestionControl = false;
});
```

### 10.2 窗口与 MTU

```csharp
builder.UseKcp(options => {
    options.SendWindow = 1024;     // 大窗口，高吞吐
    options.ReceiveWindow = 1024;
    options.Mtu = 1400;            // 默认 MTU
    // 注意：MTU 过大会导致 IP 分片丢包
});
```

### 10.3 在 ListenOptions 上的扩展

无需扩展 `ListenOptions`。KCP 监听器复用现有的 `ListenOptions.Ip` 和 `ListenOptions.Port`。KCP 特有配置通过 `KcpConnectionOptions` 独立管理。

---

## 11. 实施方案（TODO）

### 阶段一：KCP 核心移植

#### TODO-S1: 创建项目结构

- **描述**：创建 `GameFrameX.SuperSocket.Kcp` 项目目录、csproj 文件，添加到 .sln
- **涉及文件**：
  - `src/GameFrameX.SuperSocket.Kcp/GameFrameX.SuperSocket.Kcp.csproj`
  - `GameFrameX.SuperSocket.sln`
  - `src/src.sln`
- **依赖**：无
- **验收标准**：`dotnet build` 成功，项目出现在解决方案中

#### TODO-S2: 移植 KCP 常量与数据结构

- **描述**：创建 `KcpConstants.cs`（协议常量）、`KcpSegment.cs`（数据段）、`KcpSegmentManager.cs`（对象池）
- **涉及文件**：
  - `Kcp/KcpConstants.cs`
  - `Kcp/KcpSegment.cs`
  - `Kcp/KcpSegmentManager.cs`
- **依赖**：TODO-S1
- **验收标准**：编译通过，单元测试可创建和回收 Segment

#### TODO-S3: 移植 KcpCore 核心协议

- **描述**：逐函数移植 `ikcp.c` 为 `KcpCore.cs`，包含：
  - 构造/释放
  - Input / Recv / Send / Update / Flush
  - ACK 处理（parse_ack / parse_una / parse_fastack / parse_data）
  - 窗口与拥塞控制
  - 编码/解码辅助方法
  - SetNoDelay / SetWindowSize / SetMtu
- **涉及文件**：
  - `Kcp/KcpCore.cs`
- **依赖**：TODO-S2
- **验收标准**：
  - 编译通过
  - 单元测试：Send → Update → Input → Recv 能正确传递数据
  - 单元测试：丢包重传正确
  - 单元测试：乱序重组正确

### 阶段二：SuperSocket 集成

#### TODO-S4: 创建连接基础设施

- **描述**：创建 `KcpConnectionOptions`、`KcpConnectionInfo`、`IKcpSessionIdentifierProvider`、`KcpConvIdentifierProvider`
- **涉及文件**：
  - `KcpConnectionOptions.cs`
  - `KcpConnectionInfo.cs`
  - `IKcpSessionIdentifierProvider.cs`
  - `KcpConvIdentifierProvider.cs`
- **依赖**：TODO-S1
- **验收标准**：编译通过，DI 可注册和解析

#### TODO-S5: 实现 KcpPipeConnection

- **描述**：实现 `KcpPipeConnection`，包含：
  - 构造函数（初始化 KcpCore + Update 定时器）
  - `InputUdpPacket()` — UDP 包 → KCP → Pipe
  - `SendOverIOAsync()` — Pipe → KCP Send
  - `OnKcpOutput()` — KCP → Socket SendTo
  - `UpdateLoopAsync()` — 定时器驱动 KcpCore.Update
  - `Close()` — 停止定时器、清理 KCP 资源
- **涉及文件**：
  - `KcpPipeConnection.cs`
- **依赖**：TODO-S3, TODO-S4
- **验收标准**：编译通过，集成测试：数据能正确通过 KCP 收发

#### TODO-S6: 实现 KcpConnectionListener 及工厂

- **描述**：实现 `KcpConnectionListener`、`KcpConnectionListenerFactory`、`KcpConnectionFactory`、`KcpConnectionFactoryBuilder`，包含：
  - UDP Socket 监听与接收循环
  - 按 Conv 路由到已存在的 KcpPipeConnection
  - 新连接时创建 KcpPipeConnection 并触发 NewConnectionAccept
  - 异常处理与日志
- **涉及文件**：
  - `KcpConnectionListener.cs`
  - `KcpConnectionListenerFactory.cs`
  - `KcpConnectionFactory.cs`
  - `KcpConnectionFactoryBuilder.cs`
- **依赖**：TODO-S5
- **验收标准**：编译通过，集成测试：客户端能连上、收发数据、断开

#### TODO-S7: 实现 UseKcp 扩展方法

- **描述**：实现 `KcpServerHostBuilderExtensions`，包含：
  - `UseKcp()` 默认参数版
  - `UseKcp(Action<KcpConnectionOptions>)` 自定义版
  - 泛型版本 `UseKcp<TReceivePackage>`
  - DI 注册（ListenerFactory、ConnectionFactoryBuilder、SessionIdentifierProvider、SessionContainer）
- **涉及文件**：
  - `KcpServerHostBuilderExtensions.cs`
- **依赖**：TODO-S6
- **验收标准**：编译通过，完整集成测试：通过 UseKcp 启动服务器，收发自如

### 阶段三：完善与文档

#### TODO-C1: XML 文档注释

- **描述**：为所有 public/internal 类、方法、属性添加 XML 文档注释
- **涉及文件**：所有 `.cs` 文件
- **依赖**：TODO-S7
- **验收标准**：编译无 XML 注释警告

#### TODO-C2: 集成测试

- **描述**：编写集成测试项目，覆盖：
  - 基本连接与收发
  - 多客户端并发
  - 大包分片重组
  - 连接超时回收
  - 不同 KCP 模式（normal / fast）
- **涉及文件**：
  - `test/GameFrameX.SuperSocket.Kcp.Tests/` （新增测试项目）
- **依赖**：TODO-S7
- **验收标准**：所有测试通过

### 依赖关系与执行顺序

```
TODO-S1 (项目结构)
  ├──→ TODO-S2 (数据结构) ──→ TODO-S3 (KcpCore) ──→ TODO-S5 (Connection) ──→ TODO-S6 (Listener) ──→ TODO-S7 (Extensions)
  └──→ TODO-S4 (基础设施) ─────────────────────┘

TODO-S7 ──→ TODO-C1 (文档)
TODO-S7 ──→ TODO-C2 (测试)
```

**关键路径**：S1 → S2 → S3 → S5 → S6 → S7（线性依赖）
**可并行**：S2 和 S4 可并行；C1 和 C2 可并行

---

## 12. 验收标准（AC）

### AC-1：项目结构完整性

- **Given** GameFrameX.SuperSocket 解决方案
- **When** 添加 `GameFrameX.SuperSocket.Kcp` 项目
- **Then** 项目编译成功，出现在 .sln 中，与其他项目无冲突

### AC-2：KCP 核心协议正确性

- **Given** 两个 KcpCore 实例（模拟收发双方）
- **When** 一方 Send 数据，另一方 Input + Recv
- **Then** 接收方得到与发送方完全一致的数据

### AC-3：丢包重传

- **Given** 模拟 10% 丢包率的通道
- **When** 发送 1000 条消息
- **Then** 接收方最终收到全部 1000 条消息，内容一致，顺序正确

### AC-4：与 SuperSocket 管线集成

- **Given** 使用 `UseKcp()` 启动的 SuperSocket 服务器 + 自定义 PipelineFilter
- **When** 客户端通过 KCP 发送数据
- **Then** 服务器 PipelineFilter 正确解析，PackageHandler 收到正确的 Package

### AC-5：多客户端并发

- **Given** 10 个客户端同时连接同一 KCP 服务器
- **When** 各客户端独立收发 100 条消息
- **Then** 各客户端收到正确的响应，无串扰

### AC-6：连接生命周期

- **Given** 活跃的 KCP 连接
- **When** 客户端断开（停止发送）超过 IdleTimeout 秒
- **Then** 服务端自动回收连接和 Session

### AC-7：UseKcp() 一致性

- **Given** 用户代码
- **When** 将 `UseUdp()` 替换为 `UseKcp()`，其余代码不变
- **Then** 服务器正常启动，PipelineFilter 和 PackageHandler 无需任何修改

---

## 13. 测试方案

### 13.1 单元测试

| 测试类 | 覆盖点 |
|--------|--------|
| `KcpCoreTest` | Send/Recv 基本收发、分片重组、ACK 处理、窗口滑动、RTO 计算 |
| `KcpSegmentManagerTest` | 对象池 Rent/Return、边界大小、并发安全 |
| `KcpConvIdentifierProviderTest` | 正常包解析、过短包异常、Conv 提取 |

### 13.2 集成测试

| 测试场景 | 步骤 | 期望 |
|----------|------|------|
| 基本连接 | 启动 KCP Server → Client Send → Server Receive → Server Reply → Client Receive | 数据一致 |
| 大包传输 | 发送 64KB 数据 | 分片重组正确 |
| 丢包模拟 | 中间层随机丢弃 10% UDP 包 | 数据仍完整到达 |
| 乱序模拟 | 中间层打乱 UDP 包顺序 | 重组后顺序正确 |
| 连接超时 | Client 停止发送 → 等待 IdleTimeout | Server 回收 Session |
| 并发连接 | 10 Client 同时连接收发 | 无串扰、无异常 |

### 13.3 测试基础设施

创建测试辅助类 `KcpTestChannel`，模拟可配置丢包率、延迟、乱序的 UDP 通道：

```csharp
class KcpTestChannel
{
    public float PacketLossRate { get; set; }   // 丢包率 0.0~1.0
    public int MaxLatencyMs { get; set; }        // 最大延迟
    public bool ReorderPackets { get; set; }     // 是否乱序

    // 将一端 KcpCore.Output 转发给另一端 KcpCore.Input
    public void Connect(KcpCore client, KcpCore server);
}
```

---

## 14. 影响评估

### 向后兼容

- ✅ **完全兼容**：新增模块，不修改任何现有代码（仅修改 .sln 添加项目引用）
- ✅ **API 一致**：UseKcp() 与 UseUdp() 使用方式完全一致

### 数据影响

- ❌ 无数据影响（新模块，不涉及数据存储）

### 依赖影响

- ❌ 无新增外部依赖（KCP 核心自行移植）
- 内部依赖：Primitives、Connection、Server.Abstractions（已有项目）

### 性能影响

- KCP 每连接一个 `PeriodicTimer`（10ms），1000 连接 = 1000 个定时器
  - 缓解：后续可优化为全局定时器 + 时间轮调度
- KCP 发送有额外 CPU 开销（分片、ACK、重传逻辑）
  - 预期：远低于节省的网络延迟收益

### 回滚策略

- 直接删除 `GameFrameX.SuperSocket.Kcp` 项目目录
- 从 .sln 中移除项目引用
- 零影响，无需数据迁移

---

## 15. 风险与缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|---------|
| KCP 核心移植引入 bug | 中 | 高 | 逐函数对比 ikcp.c，编写完整的 KCP 核心单元测试覆盖所有分支 |
| Conv 冲突（两客户端相同 Conv） | 低 | 中 | 默认路由包含 IP:Port，降低冲突概率；文档说明 Conv 分配最佳实践 |
| 定时器过多影响性能 | 中 | 中 | 先实现每连接定时器（简单），后续可优化为时间轮 |
| 内存泄漏（KcpSegment 未回收） | 低 | 高 | 使用 using 模式 + 对象池，集成测试监控内存 |
| MTU 设置不当导致 IP 分片丢包 | 低 | 中 | 默认 MTU=1400（安全值），文档说明调整风险 |
| 与现有 ClearIdleSessionMiddleware 集成 | 低 | 低 | 复用现有空闲检测机制，KCP 连接的 LastActiveTime 由 Update 刷新 |

---

## 16. 开放问题

| # | 问题 | 优先级 | 建议 |
|---|------|--------|------|
| 1 | **Conv 分配策略**：客户端随机 vs 服务端分配？ | 中 | 首版支持客户端自选 + 文档说明最佳实践；后续可加握手协议 |
| 2 | **KCP 加密**：是否在 KCP 层内集成加密？ | 低 | Out of Scope，建议在 PipelineFilter 层处理 |
| 3 | **全局 Update 调度**：是否需要时间轮替代每连接定时器？ | 中 | 首版用 PeriodicTimer，性能测试后再决定是否优化 |
| 4 | **KCP 客户端**：是否需要配套的 KCP Client 封装？ | 中 | Out of Scope for now，可用现有 SuperSocket Client + KCP 组合 |
| 5 | **连接迁移**：NAT 切换后如何保持 KCP 会话？ | 低 | Out of Scope，需要 Conv-only 路由支持 |

---

## 17. 参考资源

| 资源 | 链接 |
|------|------|
| KCP 原始仓库 | https://github.com/skywind3000/kcp |
| ikcp.c 源码 | https://github.com/skywind3000/kcp/blob/master/ikcp.c |
| KCP 协议说明 | https://github.com/skywind3000/kcp/wiki |
| SuperSocket 官方文档 | https://docs.supersocket.net/ |
| SuperSocket GitHub | https://github.com/kerryjiang/SuperSocket |
| System.IO.Pipelines | https://learn.microsoft.com/en-us/dotnet/standard/io/pipelines |

---

## 18. 附录

### 18.A KCP 协议命令类型

| cmd 值 | 名称 | 说明 |
|--------|------|------|
| 81 (0x51) | IKCP_CMD_PUSH | 数据推送 |
| 82 (0x52) | IKCP_CMD_ACK | 确认 |
| 83 (0x53) | IKCP_CMD_WASK | 窗口探测请求 |
| 84 (0x54) | IKCP_CMD_WINS | 窗口探测响应 |

### 18.B KCP NoDelay 模式参数说明

```
ikcp_nodelay(kcp, nodelay, interval, resend, nc)

nodelay:
  0 → 默认（关闭快速重传）
  1 → 开启快速重传（跳过一定次数的 ACK 直接重传）
  2 → 极速模式（更激进的重传策略）

interval:
  Update 间隔（毫秒），默认 100ms，建议 10ms

resend:
  快速重传阈值，0 表示关闭，建议 2

nc:
  0 → 开启拥塞控制
  1 → 关闭拥塞控制（游戏推荐）
```

### 18.C 现有 Udp 模块对照表

| Udp 模块文件 | Kcp 模块对应文件 | 主要差异 |
|-------------|-----------------|---------|
| `UdpConnectionInfo.cs` | `KcpConnectionInfo.cs` | 增加 `KcpOptions` |
| `UdpConnectionFactory.cs` | `KcpConnectionFactory.cs` | 创建 `KcpPipeConnection` |
| `UdpConnectionFactoryBuilder.cs` | `KcpConnectionFactoryBuilder.cs` | 基本一致 |
| `UdpConnectionListener.cs` | `KcpConnectionListener.cs` | 路由用 Conv，数据经 KCP 处理 |
| `UdpConnectionListenerFactory.cs` | `KcpConnectionListenerFactory.cs` | 注入 `IKcpSessionIdentifierProvider` |
| `UdpServerHostBuilderExtensions.cs` | `KcpServerHostBuilderExtensions.cs` | 增加 `KcpConnectionOptions` 配置 |
| `IUdpSessionIdentifierProvider.cs` | `IKcpSessionIdentifierProvider.cs` | 接收 `ReadOnlySpan<byte>` 而非 `ArraySegment<byte>` |
| `IPAddressUdpSessionIdentifierProvider.cs` | `KcpConvIdentifierProvider.cs` | 基于 Conv 而非纯 IP:Port |
| (无对应) | `KcpPipeConnection.cs` | 新增，内含 KCP 协议栈 |
| (无对应) | `Kcp/KcpCore.cs` 等 | KCP 协议核心 |

### 18.D 命名规范对照

遵循现有项目命名规范：

| 约定 | 示例 |
|------|------|
| 命名空间 | `GameFrameX.SuperSocket.Kcp` |
| 项目名 | `GameFrameX.SuperSocket.Kcp` |
| NuGet 包名 | `GameFrameX.SuperSocket.Kcp` |
| 公开类前缀 | `Kcp` + 功能名 |
| 内部类 | `internal` 或无修饰符（默认 internal） |
| 接口 | `IKcp` + 功能名 |
| XML 文档注释 | 所有 public/internal 成员 |

---

> **文档结束**
> 如需开始实施，请确认方案后执行 `/eo-implement` 或按 TODO 顺序逐步推进。
