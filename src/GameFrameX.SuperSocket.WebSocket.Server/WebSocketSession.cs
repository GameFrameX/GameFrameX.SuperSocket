using System.Buffers;
using System.Text;
using GameFrameX.SuperSocket.Primitives;
using GameFrameX.SuperSocket.ProtoBase;
using GameFrameX.SuperSocket.Server;
using GameFrameX.SuperSocket.Server.Abstractions.Session;
using ChannelCloseReason = GameFrameX.SuperSocket.Connection.CloseReason;

namespace GameFrameX.SuperSocket.WebSocket.Server;

/// <summary>
/// WebSocket会话类，继承自AppSession并实现IHandshakeRequiredSession接口
/// </summary>
public class WebSocketSession : AppSession, IHandshakeRequiredSession
{
    /// <summary>
    /// 获取或设置握手是否完成的标志
    /// </summary>
    public bool Handshaked { get; internal set; }

    /// <summary>
    /// 获取或设置HTTP头信息
    /// </summary>
    public HttpHeader HttpHeader { get; internal set; }

    /// <summary>
    /// 获取WebSocket的路径
    /// </summary>
    public string Path
    {
        get { return HttpHeader.Path; }
    }

    /// <summary>
    /// 获取或设置WebSocket子协议
    /// </summary>
    public string SubProtocol { get; internal set; }

    /// <summary>
    /// 获取或设置子协议处理器
    /// </summary>
    internal ISubProtocolHandler SubProtocolHandler { get; set; }

    /// <summary>
    /// 获取或设置关闭握手开始时间
    /// </summary>
    public DateTime CloseHandshakeStartTime { get; private set; }

    /// <summary>
    /// 获取或设置关闭状态
    /// </summary>
    internal CloseStatus CloseStatus { get; set; }

    /// <summary>
    /// 获取或设置消息编码器
    /// </summary>
    internal IPackageEncoder<WebSocketPackage> MessageEncoder { get; set; }

    /// <summary>
    /// 关闭握手开始事件
    /// </summary>
    public event EventHandler CloseHandshakeStarted;

    /// <summary>
    /// 异步发送WebSocket数据包
    /// </summary>
    /// <param name="message">要发送的WebSocket数据包</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual ValueTask SendAsync(WebSocketPackage message, CancellationToken cancellationToken = default)
    {
        return this.Connection.SendAsync(MessageEncoder, message, cancellationToken);
    }

    /// <summary>
    /// 异步发送文本消息
    /// </summary>
    /// <param name="message">要发送的文本消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual ValueTask SendAsync(string message, CancellationToken cancellationToken = default)
    {
        return SendAsync(new WebSocketPackage
                         {
                             OpCode  = OpCode.Text,
                             Message = message
                         },
                         cancellationToken);
    }

    /// <summary>
    /// 异步发送二进制数据
    /// </summary>
    /// <param name="data">要发送的二进制数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    public override ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        return SendAsync(new WebSocketPackage
                         {
                             OpCode = OpCode.Binary,
                             Data   = new ReadOnlySequence<byte>(data)
                         },
                         cancellationToken);
    }

    /// <summary>
    /// 异步发送字节数组
    /// </summary>
    /// <param name="data">要发送的字节数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    public override ValueTask SendAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        return SendAsync(new ReadOnlySequence<byte>(data), cancellationToken);
    }

    /// <summary>
    /// 异步发送只读序列数据
    /// </summary>
    /// <param name="data">要发送的只读序列数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual ValueTask SendAsync(ReadOnlySequence<byte> data, CancellationToken cancellationToken = default)
    {
        return SendAsync(new WebSocketPackage
                         {
                             OpCode = OpCode.Binary,
                             Data   = data
                         },
                         cancellationToken);
    }

    /// <summary>
    /// 异步关闭WebSocket连接
    /// </summary>
    /// <param name="reason">关闭原因</param>
    /// <param name="reasonText">关闭原因文本</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    public ValueTask CloseAsync(CloseReason reason, string reasonText = null, CancellationToken cancellationToken = default)
    {
        var closeReasonCode = (short)reason;

        var closeStatus = new CloseStatus
                          {
                              Reason = reason
                          };

        var textEncodedLen = 0;

        if (!string.IsNullOrEmpty(reasonText))
            textEncodedLen = Encoding.UTF8.GetMaxByteCount(reasonText.Length);

        var buffer = new byte[textEncodedLen + 2];

        buffer[0] = (byte)(closeReasonCode / 256);
        buffer[1] = (byte)(closeReasonCode % 256);

        var length = 2;

        if (!string.IsNullOrEmpty(reasonText))
        {
            closeStatus.ReasonText = reasonText;
            var span = new Span<byte>(buffer, 2, buffer.Length - 2);
            length += Encoding.UTF8.GetBytes(reasonText.AsSpan(), span);
        }

        CloseStatus = closeStatus;

        CloseHandshakeStartTime = DateTime.Now;
        OnCloseHandshakeStarted();

        return SendAsync(new WebSocketPackage
                         {
                             OpCode = OpCode.Close,
                             Data   = new ReadOnlySequence<byte>(buffer, 0, length)
                         },
                         cancellationToken);
    }

    /// <summary>
    /// 触发关闭握手开始事件
    /// </summary>
    private void OnCloseHandshakeStarted()
    {
        CloseHandshakeStarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 不进行握手直接关闭连接
    /// </summary>
    internal void CloseWithoutHandshake()
    {
        base.CloseAsync(ChannelCloseReason.LocalClosing).DoNotAwait();
    }

    /// <summary>
    /// 异步关闭通道
    /// </summary>
    /// <param name="closeReason">关闭原因</param>
    /// <returns>表示异步操作的任务</returns>
    public override async ValueTask CloseAsync(ChannelCloseReason closeReason)
    {
        var closeStatus = CloseStatus;

        if (closeStatus != null)
        {
            var clientInitiated = closeStatus.RemoteInitiated;
            await base.CloseAsync(clientInitiated ? ChannelCloseReason.RemoteClosing : ChannelCloseReason.LocalClosing);
            return;
        }

        try
        {
            await CloseAsync(CloseReason.NormalClosure);
        }
        catch
        {
        }
    }

    /// <summary>
    /// 异步关闭连接
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    public override async ValueTask CloseAsync()
    {
        await CloseAsync(CloseReason.NormalClosure);
    }
}