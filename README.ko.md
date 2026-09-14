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

**인디 게임 개발자를 위한 올인원 솔루션 · 인디 개발자의 꿈을 실현**

<br />

[문서](https://gameframex.doc.alianblank.com) · [빠른 시작](#빠른-시작) · QQ 그룹: 467608841 / 233840761

<br />

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | **한국어**

</div>

## 프로젝트 개요

GameFrameX.SuperSocket은 [SuperSocket](https://github.com/kerryjiang/SuperSocket)의 GameFrameX 유지 관리 포크입니다. SuperSocket은 순수 C#으로 작성된 가볍고 확장 가능한 소켓 애플리케이션 프레임워크입니다. 소켓 사용법, 소켓 연결 유지 방법, 소켓 동작 방식을 고민하지 않고도 상시 연결 소켓 애플리케이션을 손쉽게 구축할 수 있습니다.

업스트림 아키텍처와 공개 API는 그대로 유지되므로 SuperSocket 기반으로 작성된 프로젝트는 계속 동작합니다. 여기에 더해 이 포크는 GameFrameX 게임 서버에 필요한 변경 사항을 담고 있습니다. .NET 10 빌드 타깃, DI/생성자 주입 지원, 약한 네트워크 환경의 게임 트래픽을 위한 KCP / ReliableSession 프로토콜 적용입니다.

### 기능

- 가볍고 확장 가능 — 소켓을 직접 관리하지 않고도 상시 연결 소켓 애플리케이션을 구축할 수 있습니다.
- 순수 C# — 기존 .NET 시스템 어디에나 통합할 수 있습니다.
- 파이프라인 필터와 패키지 디코더를 갖춘 프로토콜 디코딩 파이프라인.
- TCP가 기본 전송 방식이며, UDP, KCP, ReliableSession은 명시적으로 선택해야 합니다.
- UDP 데이터그램 위에서 재전송과 윈도우 제어를 통한 신뢰성 전송을 제공하는 KCP 트랜스포트.
- 논리 세션 재개, 리플레이 커서, ack 범위, 스냅샷 폴백, close/error 프레임을 위한 ReliableSession 프로토콜 프레임 계약과 바이너리 코덱.
- 커맨드 패턴 요청 처리.
- WebSocket 서버와 클라이언트, 그리고 Kestrel 통합.
- DI / 생성자 주입에 친화적인 호스트 빌더.
- .NET 10 빌드 타깃.

## 빠른 시작

### 설치

NuGet.org에서 필요한 모듈을 설치합니다:

```bash
dotnet add package GameFrameX.SuperSocket.Server
dotnet add package GameFrameX.SuperSocket.ProtoBase
```

`Kcp`와 `ReliableSession` 모듈은 다음 릴리스에 포함됩니다. 그 전까지는 소스 체크아웃에서 직접 빌드하세요:

```bash
git clone https://github.com/GameFrameX/GameFrameX.SuperSocket.git
cd GameFrameX.SuperSocket
dotnet build GameFrameX.SuperSocket.slnx
```

같은 체크아웃에서 테스트 스위트를 실행합니다:

```bash
dotnet test GameFrameX.SuperSocket.slnx
dotnet test test/GameFrameX.SuperSocket.ReliableSession.Tests/GameFrameX.SuperSocket.ReliableSession.Tests.csproj
```

## 사용 예시

### 전송 방식 선택

TCP가 여전히 기본 전송 방식입니다. UDP, KCP, ReliableSession은 명시적으로 선택해야 합니다:

| 선택 | 사용 시점 | 현재 동작 |
|:---|:---|:---|
| TCP | 표준 SuperSocket 연결 경로를 사용하려는 경우. | 서버/클라이언트의 기본 전송 방식. |
| Raw UDP | 데이터그램 전달이 필요하고 손실, 중복, 순서 뒤바뀜을 직접 감수할 수 있는 경우. | `UseUdp()` / `AsUdp(...)`로 명시적으로 선택. 비신뢰성 데이터그램 전송. |
| KCP | KCP 재전송/윈도우 제어를 통한 UDP 데이터그램 상의 신뢰성 전송이 필요한 경우. | `UseKcp(...)` / `AsKcp(...)`로 명시적으로 선택. KCP-over-TCP가 아닙니다. |
| ReliableSession | 논리 세션 재개, 리플레이 커서, ack 범위, 스냅샷 폴백, close/error 프레임을 위한 프로토콜 계약이 필요한 경우. | 프로토콜 모델과 바이너리 코덱만 제공. 런타임 하트비트, 재개 상태, 리플레이 캐시, 중복 제거 캐시, 어댑터, 비즈니스 전달은 C3에서 구현되지 않았습니다. |

### 서버: KCP 활성화

`GameFrameX.SuperSocket.Kcp`를 참조하고 기존 패키지 파이프라인과 핸들러를 그대로 유지한 뒤, 호스트 빌더에
`UseKcp(...)`를 추가합니다:

```csharp
using System.Text;
using GameFrameX.SuperSocket.Kcp;
using GameFrameX.SuperSocket.ProtoBase;
using GameFrameX.SuperSocket.Server.Host;

var builder = SuperSocketHostBuilder
    .Create<TextPackageInfo, LinePipelineFilter>()
    .UseKcp(options =>
    {
        // 설정하지 않은 nullable 옵션은 KCP 내부 기본값을 유지합니다.
        options.NoDelay = true;
        options.NoDelayLevel = 1;
        options.Interval = 10;
        options.Resend = 2;
        options.NoCongestionControl = true;
        options.SendWindow = 512;
        options.ReceiveWindow = 512;
        options.MaxDatagramSize = 4096;

        // 분 단위 패킷 블랙아웃이 예상되면 이 값을 명시적으로 높이세요.
        options.DeadLink = 120;
    })
    .UsePackageHandler(async (session, package) =>
    {
        // 디코딩된 SuperSocket 패키지를 TCP에서와 동일하게 처리합니다.
        await session.SendAsync(Encoding.UTF8.GetBytes(package.Text + "\r\n"));
    });
```

`UseKcp(...)`는 아직 등록되지 않은 경우 KCP 리스너/팩토리와 기본 인프로세스 세션 컨테이너를 등록합니다.
기본 KCP 서버 세션 ID는 원격 엔드포인트와 수신 UDP 패킷에서 읽은 KCP `Conv`로 구성됩니다. 따라서 KCP
트랜스포트 계층만으로는 엔드포인트/NAT 마이그레이션을 지원하지 않습니다.

### 클라이언트: KCP 사용

`GameFrameX.SuperSocket.Kcp`를 참조하고 `AsKcp(...)`로 `EasyClient`를 구성한 뒤, 클라이언트의 일반적인
수신/송신 API를 사용합니다:

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
    // Conv = 0이면 클라이언트가 0이 아닌 대화 ID를 생성합니다.
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

`AsKcp(...)`는 UDP 소켓을 생성하고 바인딩하며, `Conv`를 할당하거나 생성하고, `KcpPipeConnection`을
만들고, KCP 업데이트 루프를 시작하며, 해당 연결의 UDP 패킷 수신을 시작합니다. 클라이언트가 특정 로컬 UDP
엔드포인트에 바인딩해야 한다면 `AsKcp(...)` 전에 `client.LocalEndPoint`를 설정하세요.

### KCP 설정 참고 사항

- 측정된 근거가 없다면 nullable 옵션은 설정하지 않고 그대로 두세요. 설정하지 않은 값은 KCP 내부
  기본값을 유지합니다.
- 실시간 게임 스타일 트래픽에서는 `NoDelay = true`, `NoDelayLevel = 1`, `Interval = 10`, `Resend = 2`,
  그리고 조정된 송수신 윈도우가 일반적인 시작점입니다.
- 보수적인 처리량을 원한다면 기본값을 더 많이 유지하고 혼잡 제어 비활성화를 피하세요.
- `DeadLink`는 KCP 세그먼트 하나의 최대 재전송 횟수입니다. 내부 기본값은 분 단위 블랙아웃 정책이
  아니므로, 허용 조건이 더 긴 블랙아웃 내성을 요구할 때 의도적으로 높이세요.
- `IdleTimeout`은 연결 수명 계층에 속합니다. 논리 세션 복구 윈도우가 아닙니다.
- `MaxDatagramSize`는 네트워크 MTU 전략에 맞아야 합니다. 너무 큰 UDP 데이터그램은 단편화와 손실
  위험을 높입니다.

### ReliableSession 프로토콜 모델

프로토콜 프레임 계약과 바이너리 코덱이 필요할 때 `GameFrameX.SuperSocket.ReliableSession`을 참조하세요:

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

ReliableSession은 현재 다음 프레임 종류를 정의하고 검증합니다: `Hello`, `HelloAck`, `Resume`,
`ResumeAck`, `Heartbeat`, `Data`, `Ack`, `SnapshotRequest`, `Snapshot`, `Close`, `Error`.
코덱은 버퍼당 완전한 ReliableSession 프레임 하나를 기대하며, 전송 스트림 분할/프레이밍은 이후
어댑터의 몫입니다.

현재 경계:

- 아직 ReliableSession을 활성화하는 서버/클라이언트 런타임 스위치가 없습니다.
- 자동 하트비트 타이머, 재연결 루프, 재개 토큰 저장소, 리플레이 캐시, 중복 제거 캐시, 스냅샷 제공자도
  아직 포함되지 않았습니다.
- KCP는 여전히 엔드포인트와 `Conv`를 전송 세션 ID로 사용합니다. ReliableSession의 `SessionId`와
  `ResumeToken`은 향후 논리 세션 재개 계약이며, 현재의 KCP 엔드포인트 마이그레이션 지원이 아닙니다.
- C3 테스트 커버리지는 프로토콜/코덱 종단 간 커버리지로, 라이프사이클, 10초/30초/60초 블랙아웃 재개
  스크립트, 리플레이, 스냅샷 폴백, 중복/순서 뒤바뀜 프레임, ack 범위를 포함합니다. 런타임 전송 통합
  커버리지는 아닙니다.

## 아키텍처

모듈은 프로토콜 기본 요소부터 호스트까지 계층을 이룹니다:

- `Primitives` / `ProtoBase` — 기본 인터페이스와 프로토콜 디코딩.
- `Connection` / `Channel` — 하위 통신 추상화와 요청 파이프라인.
- `Server` / `Server.Abstractions` / `Client` / `ClientEngine` / `Client.Proxy` — 호스트, 서버, 클라이언트 엔드포인트.
- `Command` — 서버 위에서 동작하는 커맨드 패턴 요청 처리.
- `Udp` / `Kcp` / `ReliableSession` — 선택적 전송 방식과 논리 세션 프로토콜.
- `WebSocket` / `WebSocket.Server` / `Kestrel` / `Http` — HTTP 계열 프로토콜과 호스팅.

## 플랫폼 지원

- .NET 10.0
- Windows, macOS, Linux

## 의존성

| 모듈 | 패키지 | 설명 |
|:---|:---|:---|
| Primitives | `GameFrameX.SuperSocket.Primitives` | 기본 인터페이스와 클래스 |
| ProtoBase | `GameFrameX.SuperSocket.ProtoBase` | 프로토콜 디코딩 |
| Connection | `GameFrameX.SuperSocket.Connection` | 파이프라인을 갖춘 하위 통신 추상화 |
| Server Abstractions | `GameFrameX.SuperSocket.Server.Abstractions` | 서버 추상화 |
| Server | `GameFrameX.SuperSocket.Server` | 서버 호스트 |
| Client | `GameFrameX.SuperSocket.Client` | 클라이언트 엔드포인트 |
| Client Engine | `GameFrameX.SuperSocket.ClientEngine` | 클라이언트 엔진 |
| Client Proxy | `GameFrameX.SuperSocket.Client.Proxy` | 클라이언트 프록시 지원 |
| Command | `GameFrameX.SuperSocket.Command` | 커맨드 패턴 요청 처리 |
| Udp | `GameFrameX.SuperSocket.Udp` | UDP 전송 |
| Kcp | `GameFrameX.SuperSocket.Kcp` | UDP 기반 KCP 전송 |
| ReliableSession | `GameFrameX.SuperSocket.ReliableSession` | ReliableSession 프로토콜 모델과 코덱 |
| WebSocket | `GameFrameX.SuperSocket.WebSocket` | WebSocket 프로토콜 구현 |
| WebSocket Server | `GameFrameX.SuperSocket.WebSocket.Server` | WebSocket 서버 |
| Kestrel | `GameFrameX.SuperSocket.Kestrel` | Kestrel 통합 |
| Http | `GameFrameX.SuperSocket.Http` | HTTP 계열 프로토콜용 공용 유틸리티 |

.NET 기본 클래스 라이브러리 외에 모듈은 `Microsoft.Extensions.*`(Configuration, DependencyInjection,
Hosting, Logging, Options), `System.IO.Pipelines`, 그리고 Kestrel 모듈용 `Microsoft.AspNetCore.App`
프레임워크 참조에 의존합니다.

## 문서 및 자료

- [문서](https://gameframex.doc.alianblank.com)
- [GitHub 저장소](https://github.com/GameFrameX/GameFrameX.SuperSocket)
- [이슈 트래커](https://github.com/GameFrameX/GameFrameX.SuperSocket/issues)
- [업스트림 프로젝트](https://github.com/kerryjiang/SuperSocket)
- [업스트림 문서](https://docs.supersocket.net/)

## 커뮤니티 및 지원

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

## 변경 로그

버전 히스토리는 [Releases](https://github.com/GameFrameX/GameFrameX.SuperSocket/releases)를 참조하세요.

## 라이선스

자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.

<!--
EN: See [LICENSE](LICENSE) for license information.
zh-CN: 详见 [LICENSE](LICENSE) 文件。
zh-TW: 詳見 [LICENSE](LICENSE) 檔案。
ja: 詳しくは [LICENSE](LICENSE) をご参照ください。
ko: 자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.
-->
