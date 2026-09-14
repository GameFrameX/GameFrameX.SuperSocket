using GameFrameX.SuperSocket.ProtoBase;

namespace GameFrameX.SuperSocket.ReliableSession;

/// <summary>
/// 表示 ReliableSession 协议帧编解码过程中发生的协议错误。
/// </summary>
/// <remarks>
/// Represents a protocol error raised while encoding or decoding ReliableSession frames.
/// </remarks>
public sealed class ReliableSessionProtocolException : ProtocolException
{
    /// <summary>
    /// 使用指定错误消息初始化 <see cref="ReliableSessionProtocolException"/> 类的新实例。
    /// </summary>
    /// <remarks>
    /// Initializes a new exception instance with the specified message.
    /// </remarks>
    /// <param name="message">错误消息 / Error message</param>
    public ReliableSessionProtocolException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用指定错误消息和内部异常初始化 <see cref="ReliableSessionProtocolException"/> 类的新实例。
    /// </summary>
    /// <remarks>
    /// Initializes a new exception instance with the specified message and inner exception.
    /// </remarks>
    /// <param name="message">错误消息 / Error message</param>
    /// <param name="exception">导致当前异常的内部异常 / Inner exception that caused this exception</param>
    public ReliableSessionProtocolException(string message, Exception exception)
        : base(message, exception)
    {
    }
}
