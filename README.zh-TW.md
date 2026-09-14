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

**獨立遊戲前後端一體化解決方案 · 獨立遊戲開發者的圓夢大使**

<br />

[文檔](https://gameframex.doc.alianblank.com) · [快速開始](#快速開始) · QQ群: 467608841 / 233840761

<br />

[English](README.md) | [简体中文](README.zh-CN.md) | **繁體中文** | [日本語](README.ja.md) | [한국어](README.ko.md)

</div>

## 項目簡介

GameFrameX.SuperSocket 是 GameFrameX 維護的 [SuperSocket](https://github.com/kerryjiang/SuperSocket) 分支 —— 一套以純 C# 撰寫的輕量、可擴充的 socket 應用程式框架。你可以用它輕鬆建構永遠保持連線的 socket 應用程式，而不必煩惱該如何使用 socket、如何維護 socket 連線，以及 socket 的運作原理。

上游的架構與公開 API 均予以保留，因此針對 SuperSocket 撰寫的專案仍可正常運作。在此基礎之上，本分支納入了 GameFrameX 遊戲伺服器所需的變更：.NET 10 建置目標、DI／建構函式注入支援，以及針對弱網遊戲流量的 KCP / ReliableSession 協議調適。

### 功能特性

- 輕量且可擴充 —— 無需手動管理 socket，即可建構永遠保持連線的 socket 應用程式。
- 純 C# 實作，能整合進任何既有的 .NET 系統。
- 具備管線篩選器與封包解碼器的協議解碼管線。
- TCP 是預設的傳輸方式；UDP、KCP 與 ReliableSession 則需明確選用。
- KCP 傳輸可在 UDP 資料包之上提供可靠傳遞，具備重送與視窗控制。
- ReliableSession 協議訊框契約與二進位編解碼器，涵蓋邏輯工作階段續傳、重播游標、確認（Ack）範圍、快照回退，以及關閉／錯誤訊框。
- 命令模式的請求處理。
- WebSocket 伺服器與用戶端，以及 Kestrel 整合。
- 對 DI／建構函式注入友善的主機建置器。
- .NET 10 建置目標。

## 快速開始

### 安裝

從 NuGet.org 安裝你需要的模組：

```bash
dotnet add package GameFrameX.SuperSocket.Server
dotnet add package GameFrameX.SuperSocket.ProtoBase
```

`Kcp` 與 `ReliableSession` 模組將隨下一個版本推出；在那之前，請從原始碼簽出處自行建置：

```bash
git clone https://github.com/GameFrameX/GameFrameX.SuperSocket.git
cd GameFrameX.SuperSocket
dotnet build GameFrameX.SuperSocket.slnx
```

在同一個簽出目錄中執行測試套件：

```bash
dotnet test GameFrameX.SuperSocket.slnx
dotnet test test/GameFrameX.SuperSocket.ReliableSession.Tests/GameFrameX.SuperSocket.ReliableSession.Tests.csproj
```

## 使用範例

### 傳輸選型

TCP 仍是預設的傳輸方式。UDP、KCP 與 ReliableSession 則需明確選擇：

| 選擇 | 適用時機 | 目前行為 |
|:---|:---|:---|
| TCP | 你想要標準的 SuperSocket 連線路徑。 | 預設的伺服器／用戶端傳輸。 |
| 原始 UDP | 你想要資料包傳遞，並且能自行承受遺失、重複與亂序。 | 以 `UseUdp()` / `AsUdp(...)` 明確選用；不可靠的資料包傳輸。 |
| KCP | 你想要在 UDP 資料包之上，透過 KCP 重送／視窗控制取得可靠傳遞。 | 以 `UseKcp(...)` / `AsKcp(...)` 明確選用；並非 KCP-over-TCP。 |
| ReliableSession | 你需要一套協議契約，涵蓋邏輯工作階段續傳、重播游標、確認範圍、快照回退，以及關閉／錯誤訊框。 | 僅有協議模型與二進位編解碼器。執行階段的心跳、續傳狀態、重播快取、去重快取、配接器與業務投遞在 C3 尚未實作。 |

### 伺服器：啟用 KCP

參考 `GameFrameX.SuperSocket.Kcp`，保留你原本的封包管線與處理常式，接著在主機建置器上加入
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
        // 未設定的可空選項會保留 KCP 的內部預設值。
        options.NoDelay = true;
        options.NoDelayLevel = 1;
        options.Interval = 10;
        options.Resend = 2;
        options.NoCongestionControl = true;
        options.SendWindow = 512;
        options.ReceiveWindow = 512;
        options.MaxDatagramSize = 4096;

        // 當你預期會出現分鐘級別的封包中斷時，請明確調高此值。
        options.DeadLink = 120;
    })
    .UsePackageHandler(async (session, package) =>
    {
        // 依照你在 TCP 上的做法，處理解碼後的 SuperSocket 封包。
        await session.SendAsync(Encoding.UTF8.GetBytes(package.Text + "\r\n"));
    });
```

`UseKcp(...)` 會註冊 KCP 接聽程式／工廠，以及預設的處理程序內工作階段容器（前提是先前尚未註冊過任何容器）。預設的 KCP 伺服器工作階段身分，是由遠端端點加上從傳入 UDP 封包讀取的 KCP `Conv` 所組成。因此單靠 KCP 傳輸層，並不支援端點／NAT 遷移。

### 用戶端：使用 KCP

參考 `GameFrameX.SuperSocket.Kcp`，以 `AsKcp(...)` 設定 `EasyClient`，接著在用戶端上使用一般的接收／傳送 API：

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
    // Conv = 0 讓用戶端自行產生非零的對話識別碼。
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

`AsKcp(...)` 會建立並繫結 UDP socket、指派或產生 `Conv`、建立 `KcpPipeConnection`、啟動 KCP 更新迴圈，並開始為該連線接收 UDP 封包。當用戶端必須繫結特定的本機 UDP 端點時，請在 `AsKcp(...)` 之前先設定 `client.LocalEndPoint`。

### KCP 設定說明

- 除非你有實測依據需要調校，否則可空選項請維持未設定；未設定的值會保留 KCP 的內部預設值。
- 對於即時遊戲類型的流量，常見的起點是 `NoDelay = true`、`NoDelayLevel = 1`、`Interval = 10`、`Resend = 2`，以及調整過的傳送／接收視窗。
- 若追求保守的輸送量，請保留較多預設值，並避免停用壅塞控制。
- `DeadLink` 是單一 KCP 區段的最大重送次數。其內部預設值並非分鐘級別的封包中斷策略；當你的驗收條件要求更長的中斷容忍度時，請刻意調高它。
- `IdleTimeout` 屬於連線生命週期層，並非邏輯工作階段的復原視窗。
- `MaxDatagramSize` 應符合你的網路 MTU 策略。過大的 UDP 資料包會提高分片與遺失的風險。

### ReliableSession 協議模型

當你需要協議訊框契約與二進位編解碼器時，請參考 `GameFrameX.SuperSocket.ReliableSession`：

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

ReliableSession 目前定義並驗證以下訊框種類：`Hello`、`HelloAck`、`Resume`、`ResumeAck`、`Heartbeat`、`Data`、`Ack`、`SnapshotRequest`、`Snapshot`、`Close` 與 `Error`。編解碼器預期每個緩衝區恰好包含一個完整的 ReliableSession 訊框；傳輸串流的切分與訊框化屬於後續配接器的職責。

目前的邊界：

- 尚未有伺服器／用戶端執行階段開關能啟用 ReliableSession。
- 尚未包含自動心跳計時器、重新連線迴圈、續傳權杖儲存、重播快取、去重快取或快照提供者。
- KCP 仍以端點加上 `Conv` 作為其傳輸工作階段身分。ReliableSession 的 `SessionId` 加上 `ResumeToken` 是未來的邏輯工作階段續傳契約，並非目前 KCP 的端點遷移支援。
- C3 的測試涵蓋範圍是協議／編解碼器的端到端涵蓋，包含生命週期、10 秒／30 秒／60 秒中斷續傳腳本、重播、快照回退、重複／亂序訊框，以及確認範圍。它並非執行階段傳輸整合的涵蓋範圍。

## 架構概覽

各模組由協議基礎元件往上堆疊至主機：

- `Primitives` / `ProtoBase` —— 基礎介面與協議解碼。
- `Connection` / `Channel` —— 底層通訊抽象與請求管線。
- `Server` / `Server.Abstractions` / `Client` / `ClientEngine` / `Client.Proxy` —— 主機、伺服器與用戶端端點。
- `Command` —— 建構於伺服器之上的命令模式請求處理。
- `Udp` / `Kcp` / `ReliableSession` —— 選用的傳輸方式與邏輯工作階段協議。
- `WebSocket` / `WebSocket.Server` / `Kestrel` / `Http` —— HTTP 家族協議與裝載。

## 平台支援

- .NET 10.0
- Windows、macOS、Linux

## 依賴

| 模組 | 套件 | 說明 |
|:---|:---|:---|
| Primitives | `GameFrameX.SuperSocket.Primitives` | 基礎介面與類別 |
| ProtoBase | `GameFrameX.SuperSocket.ProtoBase` | 協議解碼 |
| Connection | `GameFrameX.SuperSocket.Connection` | 具備管線的底層通訊抽象 |
| Server Abstractions | `GameFrameX.SuperSocket.Server.Abstractions` | 伺服器抽象 |
| Server | `GameFrameX.SuperSocket.Server` | 伺服器主機 |
| Client | `GameFrameX.SuperSocket.Client` | 用戶端端點 |
| Client Engine | `GameFrameX.SuperSocket.ClientEngine` | 用戶端引擎 |
| Client Proxy | `GameFrameX.SuperSocket.Client.Proxy` | 用戶端代理支援 |
| Command | `GameFrameX.SuperSocket.Command` | 命令模式請求處理 |
| Udp | `GameFrameX.SuperSocket.Udp` | UDP 傳輸 |
| Kcp | `GameFrameX.SuperSocket.Kcp` | 基於 UDP 的 KCP 傳輸 |
| ReliableSession | `GameFrameX.SuperSocket.ReliableSession` | ReliableSession 協議模型與編解碼器 |
| WebSocket | `GameFrameX.SuperSocket.WebSocket` | WebSocket 協議實作 |
| WebSocket Server | `GameFrameX.SuperSocket.WebSocket.Server` | WebSocket 伺服器 |
| Kestrel | `GameFrameX.SuperSocket.Kestrel` | Kestrel 整合 |
| Http | `GameFrameX.SuperSocket.Http` | 類 HTTP 協議的共用工具 |

除了 .NET 基底類別庫之外，各模組還依賴 `Microsoft.Extensions.*`（Configuration、DependencyInjection、Hosting、Logging、Options）、`System.IO.Pipelines`，以及 Kestrel 模組所需的 `Microsoft.AspNetCore.App` 框架參考。

## 文檔與資源

- [文檔](https://gameframex.doc.alianblank.com)
- [GitHub 儲存庫](https://github.com/GameFrameX/GameFrameX.SuperSocket)
- [問題追蹤](https://github.com/GameFrameX/GameFrameX.SuperSocket/issues)
- [上游專案](https://github.com/kerryjiang/SuperSocket)
- [上游文檔](https://docs.supersocket.net/)

## 社區與支援

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

## 更新日誌

版本歷程請參閱 [Releases](https://github.com/GameFrameX/GameFrameX.SuperSocket/releases)。

## 開源協議

詳見 [LICENSE](LICENSE) 檔案。

<!--
EN: See [LICENSE](LICENSE) for license information.
zh-CN: 详见 [LICENSE](LICENSE) 文件。
zh-TW: 詳見 [LICENSE](LICENSE) 檔案。
ja: 詳しくは [LICENSE](LICENSE) をご参照ください。
ko: 자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.
-->
