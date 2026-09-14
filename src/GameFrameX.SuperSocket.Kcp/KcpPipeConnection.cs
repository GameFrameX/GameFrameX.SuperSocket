using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Kcp.Kcp;
using GameFrameX.SuperSocket.ProtoBase;
using GameFrameX.SuperSocket.Primitives;

namespace GameFrameX.SuperSocket.Kcp;

/// <summary>
/// 基于 KCP 协议的虚拟连接。
/// 继承 VirtualConnection，复用 SuperSocket 的 Pipe + PipelineFilter 体系。
/// 内部持有 KcpCore 实例，通过定时器驱动 KCP Update 循环。
/// </summary>
public class KcpPipeConnection : VirtualConnection, IConnectionWithSessionIdentifier
{
    private readonly Socket _socket;
    private readonly IPEndPoint _remoteEndPoint;
    private readonly KcpCore _kcp;
    private readonly CancellationTokenSource _cts;

    private readonly SemaphoreSlim _sendLock = new(1, 1);

    // KcpCore owns mutable protocol queues and must not be entered concurrently by send/input/update paths.
    private readonly object _kcpSyncRoot = new();
    private readonly bool _ownsSocket;
    private readonly bool _enableSendingPipe;
    private bool _closed;

    /// <summary>
    /// 获取 KCP 会话 ID（Conv）。
    /// </summary>
    public uint Conv
    {
        get
        {
            lock (_kcpSyncRoot)
            {
                return _kcp.Conv;
            }
        }
    }

    /// <summary>
    /// 获取会话标识。
    /// </summary>
    public string SessionIdentifier { get; }

    /// <summary>
    /// 获取远端地址。
    /// </summary>
    public new IPEndPoint RemoteEndPoint
    {
        get { return _remoteEndPoint; }
    }

    /// <summary>
    /// 初始化 KCP 连接实例。
    /// </summary>
    /// <param name="socket">关联的 UDP Socket</param>
    /// <param name="remoteEndPoint">远端地址</param>
    /// <param name="sessionIdentifier">会话标识</param>
    /// <param name="conv">KCP 会话 Conv</param>
    /// <param name="options">连接选项</param>
    /// <param name="kcpOptions">KCP 配置选项</param>
    /// <param name="ownsSocket">关闭时是否释放 Socket</param>
    public KcpPipeConnection(
        Socket socket,
        IPEndPoint remoteEndPoint,
        string sessionIdentifier,
        uint conv,
        ConnectionOptions options,
        KcpConnectionOptions kcpOptions,
        bool ownsSocket = false)
        : base(options)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _remoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
        SessionIdentifier = sessionIdentifier ?? throw new ArgumentNullException(nameof(sessionIdentifier));
        var kcpConnectionOptions = kcpOptions ?? new KcpConnectionOptions();
        _ownsSocket = ownsSocket;
        _enableSendingPipe = "true".Equals(options.Values?["enableSendingPipe"], StringComparison.OrdinalIgnoreCase);

        var segmentManager = kcpConnectionOptions.SegmentPoolSize.HasValue
                                 ? new KcpSegmentManager(kcpConnectionOptions.SegmentPoolSize.Value)
                                 : new KcpSegmentManager();
        _kcp = new KcpCore(conv == 0 ? kcpConnectionOptions.Conv : conv, segmentManager)
        {
            Output = OnKcpOutput,
        };

        // 应用 KCP 配置
        if (kcpConnectionOptions.Mtu.HasValue)
        {
            _kcp.SetMtu(kcpConnectionOptions.Mtu.Value);
        }

        if (kcpConnectionOptions.SendWindow.HasValue || kcpConnectionOptions.ReceiveWindow.HasValue)
        {
            _kcp.SetWindowSize(
                kcpConnectionOptions.SendWindow ?? KcpConstants.IKCP_WND_SND,
                kcpConnectionOptions.ReceiveWindow ?? KcpConstants.IKCP_WND_RCV);
        }

        if (kcpConnectionOptions.NoDelay.HasValue ||
            kcpConnectionOptions.NoDelayLevel.HasValue ||
            kcpConnectionOptions.Interval.HasValue ||
            kcpConnectionOptions.Resend.HasValue ||
            kcpConnectionOptions.NoCongestionControl.HasValue ||
            kcpConnectionOptions.StreamMode.HasValue)
        {
            _kcp.ConfigureNoDelay(
                kcpConnectionOptions.NoDelay,
                kcpConnectionOptions.NoDelayLevel,
                kcpConnectionOptions.Interval,
                kcpConnectionOptions.Resend,
                kcpConnectionOptions.NoCongestionControl,
                kcpConnectionOptions.StreamMode);
        }

        if (kcpConnectionOptions.DeadLink.HasValue)
        {
            _kcp.DeadLink = kcpConnectionOptions.DeadLink.Value;
        }

        if (kcpConnectionOptions.FastAckLimit.HasValue)
        {
            _kcp.SetFastAckLimit(kcpConnectionOptions.FastAckLimit.Value);
        }

        if (kcpConnectionOptions.InitialRto.HasValue || kcpConnectionOptions.MinRto.HasValue || kcpConnectionOptions.MaxRto.HasValue)
        {
            _kcp.ConfigureRto(kcpConnectionOptions.InitialRto, kcpConnectionOptions.MinRto, kcpConnectionOptions.MaxRto);
        }

        if (kcpConnectionOptions.ProbeInit.HasValue || kcpConnectionOptions.ProbeLimit.HasValue)
        {
            _kcp.SetProbeIntervals(
                kcpConnectionOptions.ProbeInit ?? KcpConstants.IKCP_PROBE_INIT,
                kcpConnectionOptions.ProbeLimit ?? KcpConstants.IKCP_PROBE_LIMIT);
        }

        if (kcpConnectionOptions.InitialCongestionWindow.HasValue)
        {
            _kcp.SetInitialCongestionWindow(kcpConnectionOptions.InitialCongestionWindow.Value);
        }

        if (kcpConnectionOptions.SlowStartThreshold.HasValue)
        {
            _kcp.SetSlowStartThreshold(kcpConnectionOptions.SlowStartThreshold.Value);
        }

        _cts = new CancellationTokenSource();
    }

    /// <summary>
    /// 接收 UDP 原始数据，送入 KCP 协议栈处理。
    /// 由 KcpConnectionListener 在收到 UDP 包时调用。
    /// </summary>
    /// <param name="data">UDP 原始数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async ValueTask InputUdpPacketAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        if (_closed)
        {
            return;
        }

        List<byte[]> receivedPayloads = null;

        try
        {
            lock (_kcpSyncRoot)
            {
                _kcp.Input(data.Span);

                // 从 KCP 接收缓冲区取出完整消息，写入上层 Pipe
                while (_kcp.PeekCanRecv())
                {
                    var peekSize = _kcp.PeekSize();
                    if (peekSize <= 0)
                    {
                        break;
                    }

                    var buffer = new byte[peekSize];
                    var received = _kcp.Recv(buffer);
                    if (received > 0)
                    {
                        if (received != buffer.Length)
                        {
                            Array.Resize(ref buffer, received);
                        }

                        receivedPayloads ??= new List<byte[]>();
                        receivedPayloads.Add(buffer);
                    }
                }
            }

            if (receivedPayloads == null)
            {
                return;
            }

            foreach (var payload in receivedPayloads)
            {
                await WriteInputPipeDataAsync(payload, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
            // KCP 协议处理错误，忽略此包
        }
    }

    /// <summary>
    /// 启动 KCP Update 定时循环。
    /// 应在连接创建后调用。
    /// </summary>
    public void StartUpdateLoop()
    {
        UpdateLoopAsync().DoNotAwait();
    }

    /// <summary>
    /// KCP Update 循环，按配置的间隔驱动 KCP 状态机。
    /// </summary>
    private async Task UpdateLoopAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var current = (uint)(Environment.TickCount64 & 0xFFFFFFFF);
                uint nextUpdate;

                lock (_kcpSyncRoot)
                {
                    _kcp.Update(current);
                    nextUpdate = _kcp.Check(current);
                }

                var delay = Math.Max(1, (int)(nextUpdate - current));

                await Task.Delay(delay, _cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常关闭
        }
        catch (Exception)
        {
            // 异常退出
        }
    }

    /// <summary>
    /// KCP 需要发送 UDP 包时的回调。
    /// </summary>
    private void OnKcpOutput(Memory<byte> data)
    {
        if (_closed || _socket == null)
        {
            return;
        }

        try
        {
            _socket.SendTo(data.Span, SocketFlags.None, _remoteEndPoint);
        }
        catch (Exception)
        {
            // 发送失败，忽略
        }
    }

    /// <summary>
    /// 关闭连接并释放资源。
    /// </summary>
    protected override void Close()
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        _cts.Cancel();
        Input.Writer.Complete();

        if (_ownsSocket)
        {
            _socket.Dispose();
        }
    }

    /// <summary>
    /// 不支持从 Socket 直接读取（数据由 Listener 投递）。
    /// </summary>
    protected override ValueTask<int> FillInputPipeWithDataAsync(Memory<byte> memory, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// 将上层 Pipe 数据通过 KCP 发送。
    /// </summary>
    protected override async ValueTask<int> SendOverIOAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        if (_closed)
        {
            return 0;
        }

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (buffer.IsSingleSegment)
            {
                lock (_kcpSyncRoot)
                {
                    return _kcp.Send(buffer.First.Span);
                }
            }

            // 合并多段数据
            var totalLength = (int)buffer.Length;
            var pooledBuffer = ArrayPool<byte>.Shared.Rent(totalLength);
            try
            {
                var offset = 0;
                foreach (var segment in buffer)
                {
                    segment.Span.CopyTo(pooledBuffer.AsSpan(offset));
                    offset += segment.Length;
                }

                lock (_kcpSyncRoot)
                {
                    return _kcp.Send(pooledBuffer.AsSpan(0, totalLength));
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pooledBuffer);
            }
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// 处理发送管道。KCP 不使用 Output Pipe 发送模式。
    /// </summary>
    protected override Task ProcessSends()
    {
        if (_enableSendingPipe)
        {
            return base.ProcessSends();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 发送数据。
    /// </summary>
    public override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_enableSendingPipe)
        {
            await base.SendAsync(buffer, cancellationToken).ConfigureAwait(false);
            return;
        }

        await SendOverIOAsync(new ReadOnlySequence<byte>(buffer), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 发送数据。
    /// </summary>
    public override async ValueTask SendAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_enableSendingPipe)
        {
            await base.SendAsync(buffer, cancellationToken).ConfigureAwait(false);
            return;
        }

        await SendOverIOAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 使用编码器发送包。
    /// </summary>
    public override async ValueTask SendAsync<TPackage>(IPackageEncoder<TPackage> packageEncoder, TPackage package, CancellationToken cancellationToken = default)
    {
        if (_enableSendingPipe)
        {
            await base.SendAsync(packageEncoder, package, cancellationToken).ConfigureAwait(false);
            return;
        }

        var writer = new ArrayBufferWriter<byte>();
        WritePackageWithEncoder<TPackage>(writer, packageEncoder, package);
        await SendOverIOAsync(new ReadOnlySequence<byte>(writer.WrittenMemory), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 使用写入 Action 发送数据（非 SendingPipe 模式不支持）。
    /// </summary>
    public override ValueTask SendAsync(Action<PipeWriter> write, CancellationToken cancellationToken)
    {
        if (_enableSendingPipe)
        {
            return base.SendAsync(write, cancellationToken);
        }

        throw new NotSupportedException("The method SendAsync(Action<PipeWriter> write) cannot be used when enableSendingPipe is false.");
    }
}
