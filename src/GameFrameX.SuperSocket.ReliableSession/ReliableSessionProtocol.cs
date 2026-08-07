namespace GameFrameX.SuperSocket.ReliableSession;

/// <summary>
/// 提供 ReliableSession 协议级常量。
/// </summary>
/// <remarks>
/// Provides protocol-wide constants fixed by the C3 protocol model and shared by later runtime and adapter work.
/// </remarks>
public static class ReliableSessionProtocol
{
    /// <summary>
    /// 获取当前 ReliableSession 编解码器使用的线格式版本。
    /// </summary>
    /// <remarks>
    /// Gets the current wire version used by the ReliableSession codec.
    /// </remarks>
    /// <value>当前线格式版本 / Current wire-format version</value>
    public const byte WireVersion = 1;

    /// <summary>
    /// 获取固定协议帧头的字节长度。
    /// </summary>
    /// <remarks>
    /// Gets the size in bytes of the fixed frame header.
    /// </remarks>
    /// <value>固定帧头字节长度 / Fixed frame header size in bytes</value>
    public const int FrameHeaderSize = 8;
}
