namespace GameFrameX.SuperSocket.ReliableSession;

/// <summary>
/// 表示每个 ReliableSession 协议帧前置的固定头。
/// </summary>
/// <remarks>
/// Represents the fixed header used by the codec to validate wire version, frame kind, flags, and body length before decoding the frame body.
/// </remarks>
/// <param name="Version">协议线格式版本 / Wire-format version</param>
/// <param name="Kind">协议帧类型 / Protocol frame kind</param>
/// <param name="Flags">编码器使用的标志位 / Codec flags</param>
/// <param name="BodyLength">帧体字节长度 / Frame body length in bytes</param>
public readonly record struct ReliableSessionFrameHeader(byte Version, ReliableSessionFrameKind Kind, byte Flags, int BodyLength);
