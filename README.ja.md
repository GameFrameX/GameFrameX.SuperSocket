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

**インディゲーム開発者向けオールインワンソリューション · インディ開発者の夢を支援**

<br />

[ドキュメント](https://gameframex.doc.alianblank.com) · [クイックスタート](#クイックスタート) · QQグループ: 467608841 / 233840761

<br />

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | **日本語** | [한국어](README.ko.md)

</div>

## プロジェクト概要

GameFrameX.SuperSocket は、純粋な C# で書かれた軽量で拡張可能なソケットアプリケーションフレームワーク [SuperSocket](https://github.com/kerryjiang/SuperSocket) を GameFrameX がメンテナンスするフォークです。socket の使い方やソケット接続の維持方法、socket の仕組みを意識することなく、常時接続型のソケットアプリケーションを簡単に構築できます。

アップストリームのアーキテクチャと公開 API はそのまま維持されているため、SuperSocket 向けに書かれたプロジェクトは引き続き動作します。さらに本フォークは GameFrameX のゲームサーバーが必要とする変更を搭載しています。すなわち .NET 10 ビルドターゲット、DI/コンストラクタインジェクションのサポート、そして弱回線のゲームトラフィック向けの KCP / ReliableSession プロトコル対応です。

### 機能概要

- 軽量で拡張可能 — ソケットを手動で管理することなく、常時接続型のソケットアプリケーションを構築できます。
- 純粋な C# 製なので、既存のどの .NET システムにも統合できます。
- パイプラインフィルターとパッケージデコーダーによるプロトコルデコードパイプライン。
- デフォルトのトランスポートは TCP で、UDP、KCP、ReliableSession は明示的なオプトインです。
- UDP データグラム上で信頼性のある配信を実現する KCP トランスポート。再送とウィンドウ制御を備えています。
- 論理セッションの復帰、リプレイカーソル、ack レンジ、スナップショットフォールバック、close/error フレームのための ReliableSession プロトコルフレーム契約とバイナリコーデック。
- コマンドパターンによるリクエスト処理。
- WebSocket サーバーとクライアント、および Kestrel 統合。
- DI / コンストラクタインジェクションに対応したホストビルダー。
- .NET 10 ビルドターゲット。

## クイックスタート

### インストール

必要なモジュールを NuGet.org からインストールします：

```bash
dotnet add package GameFrameX.SuperSocket.Server
dotnet add package GameFrameX.SuperSocket.ProtoBase
```

`Kcp` と `ReliableSession` モジュールは次のリリースに含まれます。それまではソースチェックアウトからビルドしてください：

```bash
git clone https://github.com/GameFrameX/GameFrameX.SuperSocket.git
cd GameFrameX.SuperSocket
dotnet build GameFrameX.SuperSocket.slnx
```

同じチェックアウトでテストスイートを実行します：

```bash
dotnet test GameFrameX.SuperSocket.slnx
dotnet test test/GameFrameX.SuperSocket.ReliableSession.Tests/GameFrameX.SuperSocket.ReliableSession.Tests.csproj
```

## 使用例

### トランスポートの選択

TCP は引き続きデフォルトのトランスポートです。UDP、KCP、ReliableSession は明示的な選択です：

| 選択肢 | 使用する場面 | 現在の動作 |
|:---|:---|:---|
| TCP | 標準の SuperSocket 接続パスを使いたい場合。 | デフォルトのサーバー/クライアントトランスポート。 |
| Raw UDP | データグラム配信を使い、損失・重複・順序入れ替えを自分で許容できる場合。 | `UseUdp()` / `AsUdp(...)` による明示的なオプトイン。非信頼のデータグラムトランスポート。 |
| KCP | KCP の再送/ウィンドウ制御を伴う、UDP データグラム上の信頼性配信を使いたい場合。 | `UseKcp(...)` / `AsKcp(...)` による明示的なオプトイン。KCP-over-TCP ではありません。 |
| ReliableSession | 論理セッションの復帰、リプレイカーソル、ack レンジ、スナップショットフォールバック、close/error フレームのためのプロトコル契約が必要な場合。 | プロトコルモデルとバイナリコーデックのみ。ランタイムのハートビート、復帰状態、リプレイキャッシュ、重複排除キャッシュ、アダプター、ビジネス配信は C3 では実装されていません。 |

### サーバー：KCP を有効化

`GameFrameX.SuperSocket.Kcp` を参照し、通常のパッケージパイプラインとハンドラーを維持したまま、ホストビルダーに `UseKcp(...)` を追加します：

```csharp
using System.Text;
using GameFrameX.SuperSocket.Kcp;
using GameFrameX.SuperSocket.ProtoBase;
using GameFrameX.SuperSocket.Server.Host;

var builder = SuperSocketHostBuilder
    .Create<TextPackageInfo, LinePipelineFilter>()
    .UseKcp(options =>
    {
        // 未設定の nullable オプションは KCP 内部のデフォルト値を保つ
        options.NoDelay = true;
        options.NoDelayLevel = 1;
        options.Interval = 10;
        options.Resend = 2;
        options.NoCongestionControl = true;
        options.SendWindow = 512;
        options.ReceiveWindow = 512;
        options.MaxDatagramSize = 4096;

        // 分単位のパケットブラックアウトを見込む場合は明示的に引き上げる
        options.DeadLink = 120;
    })
    .UsePackageHandler(async (session, package) =>
    {
        // デコード済みの SuperSocket パッケージを TCP と全く同じように処理する
        await session.SendAsync(Encoding.UTF8.GetBytes(package.Text + "\r\n"));
    });
```

`UseKcp(...)` は KCP リスナー/ファクトリーと、まだ登録されていない場合のデフォルトのインプロセスセッションコンテナーを登録します。デフォルトの KCP サーバーセッション ID は、リモートエンドポイントと受信 UDP パケットから読み取った KCP の `Conv` から構成されます。そのため、エンドポイント/NAT のマイグレーションは KCP トランスポート層だけではサポートされません。

### クライアント：KCP を使用

`GameFrameX.SuperSocket.Kcp` を参照し、`EasyClient` を `AsKcp(...)` で設定してから、クライアントの通常の受信/送信 API を使用します：

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
    // Conv = 0 にするとクライアントが非ゼロの会話 ID を生成する
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

`AsKcp(...)` は UDP ソケットを作成してバインドし、`Conv` を割り当てまたは生成し、`KcpPipeConnection` を作成し、KCP 更新ループを開始し、その接続の UDP パケット受信を開始します。クライアントが特定のローカル UDP エンドポイントにバインドする必要がある場合は、`AsKcp(...)` の前に `client.LocalEndPoint` を設定してください。

### KCP 設定の注意点

- 測定に基づく調整理由がない限り、nullable オプションは未設定のままにしてください。未設定の値は KCP 内部のデフォルトを保ちます。
- リアルタイムゲームのようなトラフィックでは、一般的な出発点は `NoDelay = true`、`NoDelayLevel = 1`、`Interval = 10`、`Resend = 2`、および調整した送受信ウィンドウです。
- 保守的なスループットを重視する場合は、より多くのデフォルトを維持し、輻輳制御を無効化しないでください。
- `DeadLink` は 1 つの KCP セグメントに対する最大再送回数です。内部デフォルトは分単位のブラックアウトポリシーではありません。許容条件としてより長いブラックアウト耐性が必要な場合は、意図的に引き上げてください。
- `IdleTimeout` は接続ライフタイム層に属します。論理セッションの復旧ウィンドウではありません。
- `MaxDatagramSize` はネットワーク MTU 戦略に適合させる必要があります。大きすぎる UDP データグラムはフラグメンテーションと損失のリスクを高めます。

### ReliableSession プロトコルモデル

プロトコルフレーム契約とバイナリコーデックが必要な場合は `GameFrameX.SuperSocket.ReliableSession` を参照します：

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

ReliableSession は現在、次のフレーム種別を定義・検証しています：`Hello`、`HelloAck`、`Resume`、`ResumeAck`、`Heartbeat`、`Data`、`Ack`、`SnapshotRequest`、`Snapshot`、`Close`、`Error`。コーデックはバッファーごとに 1 つの完全な ReliableSession フレームを想定しており、トランスポートストリームの分割/フレーミングは後続のアダプターが担います。

現在の境界：

- ReliableSession を有効化するサーバー/クライアントのランタイムスイッチはまだありません。
- 自動ハートビートタイマー、再接続ループ、復帰トークンストア、リプレイキャッシュ、重複排除キャッシュ、スナップショットプロバイダーはまだ含まれていません。
- KCP は依然としてエンドポイントと `Conv` をトランスポートセッション ID として使用します。ReliableSession の `SessionId` と `ResumeToken` は将来の論理セッション復帰契約であり、現在の KCP エンドポイントマイグレーションのサポートではありません。
- C3 のテストカバレッジはプロトコル/コーデックのエンドツーエンドカバレッジであり、ライフサイクル、10s/30s/60s のブラックアウト復帰スクリプト、リプレイ、スナップショットフォールバック、重複/順序入れ替えフレーム、ack レンジを含みます。ランタイムトランスポート統合のカバレッジではありません。

## アーキテクチャ

モジュールはプロトコルプリミティブからホストまで階層化されています：

- `Primitives` / `ProtoBase` — プリミティブインターフェースとプロトコルデコード。
- `Connection` / `Channel` — 基盤となる通信抽象とリクエストパイプライン。
- `Server` / `Server.Abstractions` / `Client` / `ClientEngine` / `Client.Proxy` — ホスト、サーバー、クライアントエンドポイント。
- `Command` — サーバー上のコマンドパターンによるリクエスト処理。
- `Udp` / `Kcp` / `ReliableSession` — オプションのトランスポートと論理セッションプロトコル。
- `WebSocket` / `WebSocket.Server` / `Kestrel` / `Http` — HTTP 系プロトコルとホスティング。

## プラットフォーム対応

- .NET 10.0
- Windows、macOS、Linux

## 依存関係

| モジュール | パッケージ | 説明 |
|:---|:---|:---|
| Primitives | `GameFrameX.SuperSocket.Primitives` | プリミティブインターフェースとクラス |
| ProtoBase | `GameFrameX.SuperSocket.ProtoBase` | プロトコルデコード |
| Connection | `GameFrameX.SuperSocket.Connection` | パイプラインを備えた基盤の通信抽象 |
| Server Abstractions | `GameFrameX.SuperSocket.Server.Abstractions` | サーバー抽象 |
| Server | `GameFrameX.SuperSocket.Server` | サーバーホスト |
| Client | `GameFrameX.SuperSocket.Client` | クライアントエンドポイント |
| Client Engine | `GameFrameX.SuperSocket.ClientEngine` | クライアントエンジン |
| Client Proxy | `GameFrameX.SuperSocket.Client.Proxy` | クライアントプロキシサポート |
| Command | `GameFrameX.SuperSocket.Command` | コマンドパターンによるリクエスト処理 |
| Udp | `GameFrameX.SuperSocket.Udp` | UDP トランスポート |
| Kcp | `GameFrameX.SuperSocket.Kcp` | UDP 上の KCP トランスポート |
| ReliableSession | `GameFrameX.SuperSocket.ReliableSession` | ReliableSession プロトコルモデルとコーデック |
| WebSocket | `GameFrameX.SuperSocket.WebSocket` | WebSocket プロトコル実装 |
| WebSocket Server | `GameFrameX.SuperSocket.WebSocket.Server` | WebSocket サーバー |
| Kestrel | `GameFrameX.SuperSocket.Kestrel` | Kestrel 統合 |
| Http | `GameFrameX.SuperSocket.Http` | HTTP 系プロトコル向けの共通ユーティリティ |

.NET 基底クラスライブラリ以外に、各モジュールは `Microsoft.Extensions.*`（Configuration、DependencyInjection、Hosting、Logging、Options）、`System.IO.Pipelines`、および Kestrel モジュール用の `Microsoft.AspNetCore.App` フレームワーク参照に依存しています。

## ドキュメントとリソース

- [ドキュメント](https://gameframex.doc.alianblank.com)
- [GitHub リポジトリ](https://github.com/GameFrameX/GameFrameX.SuperSocket)
- [イシュートラッカー](https://github.com/GameFrameX/GameFrameX.SuperSocket/issues)
- [アップストリームプロジェクト](https://github.com/kerryjiang/SuperSocket)
- [アップストリームドキュメント](https://docs.supersocket.net/)

## コミュニティとサポート

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

## 変更履歴

バージョン履歴については [Releases](https://github.com/GameFrameX/GameFrameX.SuperSocket/releases) をご参照ください。

## ライセンス

詳しくは [LICENSE](LICENSE) をご参照ください。

<!--
EN: See [LICENSE](LICENSE) for license information.
zh-CN: 详见 [LICENSE](LICENSE) 文件。
zh-TW: 詳見 [LICENSE](LICENSE) 檔案。
ja: 詳しくは [LICENSE](LICENSE) をご参照ください。
ko: 자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.
-->
