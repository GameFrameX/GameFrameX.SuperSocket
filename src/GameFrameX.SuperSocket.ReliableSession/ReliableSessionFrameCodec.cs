using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using GameFrameX.SuperSocket.ProtoBase;

namespace GameFrameX.SuperSocket.ReliableSession;

/// <summary>
/// 提供 ReliableSession 协议帧的二进制编解码能力。
/// </summary>
/// <remarks>
/// Provides binary encoding and decoding for the C3 ReliableSession protocol model; the codec expects one complete frame per buffer and does not implement transport stream framing or runtime replay logic.
/// </remarks>
public sealed class ReliableSessionFrameCodec
{
    private const byte FlagHasSnapshotVersion = 1 << 0;
    private const byte FlagSnapshotRequired = 1 << 1;
    private const byte FlagRotateResumeToken = 1 << 2;
    private const byte FlagHasErrorCode = 1 << 3;
    private const byte FlagHasCloseReason = 1 << 4;
    private const byte FlagReplayWindowInsufficient = 1 << 5;

    /// <summary>
    /// 将 ReliableSession 协议帧编码为字节数组。
    /// </summary>
    /// <remarks>
    /// Encodes a protocol frame and validates identifiers, resume tokens, acknowledgement ranges, and snapshot flags before writing bytes.
    /// </remarks>
    /// <param name="frame">要编码的协议帧 / Protocol frame to encode</param>
    /// <returns>编码后的协议帧字节 / Encoded protocol frame bytes</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="frame"/> 为 null 时抛出 / Thrown when <paramref name="frame"/> is null</exception>
    /// <exception cref="ReliableSessionProtocolException">当协议帧字段不合法或帧类型不受支持时抛出 / Thrown when frame fields are invalid or the frame type is unsupported</exception>
    public byte[] Encode(ReliableSessionFrame frame)
    {
        if (frame == null)
            throw new ArgumentNullException(nameof(frame));

        ValidateFrame(frame);

        var body = new ArrayBufferWriter<byte>();
        byte flags = 0;

        switch (frame)
        {
            case ReliableSessionHelloFrame hello:
                EncodeHello(body, hello);
                break;
            case ReliableSessionHelloAckFrame helloAck:
                EncodeHelloAck(body, helloAck);
                break;
            case ReliableSessionResumeFrame resume:
                flags = EncodeResume(body, resume);
                break;
            case ReliableSessionResumeAckFrame resumeAck:
                flags = EncodeResumeAck(body, resumeAck);
                break;
            case ReliableSessionHeartbeatFrame heartbeat:
                EncodeHeartbeat(body, heartbeat);
                break;
            case ReliableSessionDataFrame data:
                flags = EncodeData(body, data);
                break;
            case ReliableSessionAckFrame ack:
                EncodeAck(body, ack);
                break;
            case ReliableSessionSnapshotRequestFrame snapshotRequest:
                flags = EncodeSnapshotRequest(body, snapshotRequest);
                break;
            case ReliableSessionSnapshotFrame snapshot:
                EncodeSnapshot(body, snapshot);
                break;
            case ReliableSessionCloseFrame close:
                flags = EncodeClose(body, close);
                break;
            case ReliableSessionErrorFrame error:
                flags = EncodeError(body, error);
                break;
            default:
                throw new ReliableSessionProtocolException($"Unsupported frame type: {frame.GetType().FullName}.");
        }

        var output = new ArrayBufferWriter<byte>(ReliableSessionProtocol.FrameHeaderSize + body.WrittenCount);
        WriteByte(output, ReliableSessionProtocol.WireVersion);
        WriteByte(output, (byte)frame.Kind);
        WriteByte(output, flags);
        WriteByte(output, 0);
        WriteInt32(output, body.WrittenCount);
        WriteBytes(output, body.WrittenSpan);
        return output.WrittenMemory.ToArray();
    }

    /// <summary>
    /// 从包含一个完整协议帧的字节数组中解码 ReliableSession 帧。
    /// </summary>
    /// <remarks>
    /// Decodes exactly one complete frame and validates wire version, body length, identifiers, resume token, acknowledgement ranges, and snapshot flags.
    /// </remarks>
    /// <param name="buffer">包含完整协议帧的字节数组 / Byte array containing one complete protocol frame</param>
    /// <returns>解码后的协议帧 / Decoded protocol frame</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="buffer"/> 为 null 时抛出 / Thrown when <paramref name="buffer"/> is null</exception>
    /// <exception cref="ReliableSessionProtocolException">当缓冲区不完整、版本不支持、字段不合法或包含多余字节时抛出 / Thrown when the buffer is incomplete, the version is unsupported, fields are invalid, or trailing bytes exist</exception>
    public ReliableSessionFrame Decode(byte[] buffer)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        return Decode(new ReadOnlySequence<byte>(buffer));
    }

    /// <summary>
    /// 从包含一个完整协议帧的只读序列中解码 ReliableSession 帧。
    /// </summary>
    /// <remarks>
    /// Decodes exactly one complete frame from a sequence; splitting or coalescing transport stream data belongs to later adapters, not to the C3 codec.
    /// </remarks>
    /// <param name="buffer">包含完整协议帧的只读字节序列 / Read-only byte sequence containing one complete protocol frame</param>
    /// <returns>解码后的协议帧 / Decoded protocol frame</returns>
    /// <exception cref="ReliableSessionProtocolException">当缓冲区不完整、版本不支持、字段不合法或包含多余字节时抛出 / Thrown when the buffer is incomplete, the version is unsupported, fields are invalid, or trailing bytes exist</exception>
    public ReliableSessionFrame Decode(ReadOnlySequence<byte> buffer)
    {
        // ponytail: the codec expects one complete frame per buffer; stream splitting belongs in the transport adapter.
        if (buffer.Length < ReliableSessionProtocol.FrameHeaderSize)
            throw new ReliableSessionProtocolException("The buffer is too short to contain a ReliableSession frame.");

        var reader = new SequenceReader<byte>(buffer);
        var header = ReadHeader(ref reader);

        if (header.Version != ReliableSessionProtocol.WireVersion)
            throw new ReliableSessionProtocolException($"Unsupported ReliableSession wire version: {header.Version}.");

        if (header.BodyLength < 0)
            throw new ReliableSessionProtocolException("The ReliableSession frame body length is invalid.");

        if (buffer.Length != ReliableSessionProtocol.FrameHeaderSize + header.BodyLength)
            throw new ReliableSessionProtocolException("The buffer does not contain exactly one ReliableSession frame.");

        var body = buffer.Slice(reader.Consumed, header.BodyLength);
        var bodyReader = new SequenceReader<byte>(body);
        ReliableSessionFrame frame = header.Kind switch
        {
            ReliableSessionFrameKind.Hello => DecodeHello(ref bodyReader),
            ReliableSessionFrameKind.HelloAck => DecodeHelloAck(ref bodyReader),
            ReliableSessionFrameKind.Resume => DecodeResume(ref bodyReader, header.Flags),
            ReliableSessionFrameKind.ResumeAck => DecodeResumeAck(ref bodyReader, header.Flags),
            ReliableSessionFrameKind.Heartbeat => DecodeHeartbeat(ref bodyReader),
            ReliableSessionFrameKind.Data => DecodeData(ref bodyReader, header.Flags),
            ReliableSessionFrameKind.Ack => DecodeAck(ref bodyReader),
            ReliableSessionFrameKind.SnapshotRequest => DecodeSnapshotRequest(ref bodyReader, header.Flags),
            ReliableSessionFrameKind.Snapshot => DecodeSnapshot(ref bodyReader),
            ReliableSessionFrameKind.Close => DecodeClose(ref bodyReader, header.Flags),
            ReliableSessionFrameKind.Error => DecodeError(ref bodyReader, header.Flags),
            _ => throw new ReliableSessionProtocolException($"Unsupported ReliableSession frame kind: {header.Kind}.")
        };

        if (bodyReader.Remaining != 0)
            throw new ReliableSessionProtocolException("The ReliableSession frame body contains trailing bytes.");

        ValidateFrame(frame);
        return frame;
    }

    private static ReliableSessionFrameHeader ReadHeader(ref SequenceReader<byte> reader)
    {
        if (!reader.TryRead(out byte version))
            throw new ReliableSessionProtocolException("The ReliableSession frame header is incomplete.");

        if (!reader.TryRead(out byte kindValue))
            throw new ReliableSessionProtocolException("The ReliableSession frame header is incomplete.");

        if (!reader.TryRead(out byte flags))
            throw new ReliableSessionProtocolException("The ReliableSession frame header is incomplete.");

        if (!reader.TryRead(out byte _))
            throw new ReliableSessionProtocolException("The ReliableSession frame header is incomplete.");

        if (!reader.TryReadLittleEndian(out uint bodyLength))
            throw new ReliableSessionProtocolException("The ReliableSession frame header is incomplete.");

        if (bodyLength > int.MaxValue)
            throw new ReliableSessionProtocolException("The ReliableSession frame body length is out of range.");

        return new ReliableSessionFrameHeader(version, (ReliableSessionFrameKind)kindValue, flags, (int)bodyLength);
    }

    private static void EncodeHello(IBufferWriter<byte> writer, ReliableSessionHelloFrame frame)
    {
        frame.RequestedOptions.Validate();
        WriteUInt16(writer, frame.ProtocolVersion);
        WriteGuid(writer, frame.ClientInstanceId.Value);
        EncodeHandshakeOptions(writer, frame.RequestedOptions);
    }

    private static ReliableSessionHelloFrame DecodeHello(ref SequenceReader<byte> reader)
    {
        var protocolVersion = ReadUInt16(ref reader, nameof(ReliableSessionHelloFrame.ProtocolVersion));
        var clientInstanceId = new ClientInstanceId(ReadGuid(ref reader, nameof(ReliableSessionHelloFrame.ClientInstanceId)));
        var options = DecodeHandshakeOptions(ref reader);
        return new ReliableSessionHelloFrame
        {
            ProtocolVersion = protocolVersion,
            ClientInstanceId = clientInstanceId,
            RequestedOptions = options
        };
    }

    private static void EncodeHelloAck(IBufferWriter<byte> writer, ReliableSessionHelloAckFrame frame)
    {
        ValidateSessionId(frame.SessionId);
        ValidateConnectionId(frame.ConnectionId);
        ValidateResumeToken(frame.ResumeToken);
        frame.NegotiatedOptions.Validate();

        WriteGuid(writer, frame.SessionId.Value);
        WriteGuid(writer, frame.ConnectionId.Value);
        WriteUInt64(writer, frame.ResumeGeneration.Value);
        WriteString(writer, frame.ResumeToken.Value);
        EncodeHandshakeOptions(writer, frame.NegotiatedOptions);
    }

    private static ReliableSessionHelloAckFrame DecodeHelloAck(ref SequenceReader<byte> reader)
    {
        var sessionId = new SessionId(ReadGuid(ref reader, nameof(ReliableSessionHelloAckFrame.SessionId)));
        var connectionId = new ConnectionId(ReadGuid(ref reader, nameof(ReliableSessionHelloAckFrame.ConnectionId)));
        var resumeGeneration = new ResumeGeneration(ReadUInt64(ref reader, nameof(ReliableSessionHelloAckFrame.ResumeGeneration)));
        var resumeToken = new ResumeToken(ReadString(ref reader, nameof(ReliableSessionHelloAckFrame.ResumeToken)));
        var options = DecodeHandshakeOptions(ref reader);
        return new ReliableSessionHelloAckFrame
        {
            SessionId = sessionId,
            ConnectionId = connectionId,
            ResumeGeneration = resumeGeneration,
            ResumeToken = resumeToken,
            NegotiatedOptions = options
        };
    }

    private static byte EncodeResume(IBufferWriter<byte> writer, ReliableSessionResumeFrame frame)
    {
        ValidateSessionId(frame.SessionId);
        ValidateConnectionId(frame.ConnectionId);
        ValidateClientInstanceId(frame.ClientInstanceId);
        ValidateResumeToken(frame.ResumeToken);
        WriteGuid(writer, frame.SessionId.Value);
        WriteGuid(writer, frame.ConnectionId.Value);
        WriteGuid(writer, frame.ClientInstanceId.Value);
        WriteString(writer, frame.ResumeToken.Value);
        WriteUInt64(writer, frame.ReceiveCursor.Value);
        WriteUInt64(writer, frame.CommittedCursor.Value);

        byte flags = 0;
        if (frame.HasSnapshotVersion)
        {
            flags |= FlagHasSnapshotVersion;
            WriteInt64(writer, frame.SnapshotVersion.Value);
        }

        return flags;
    }

    private static ReliableSessionResumeFrame DecodeResume(ref SequenceReader<byte> reader, byte flags)
    {
        var sessionId = new SessionId(ReadGuid(ref reader, nameof(ReliableSessionResumeFrame.SessionId)));
        var connectionId = new ConnectionId(ReadGuid(ref reader, nameof(ReliableSessionResumeFrame.ConnectionId)));
        var clientInstanceId = new ClientInstanceId(ReadGuid(ref reader, nameof(ReliableSessionResumeFrame.ClientInstanceId)));
        var resumeToken = new ResumeToken(ReadString(ref reader, nameof(ReliableSessionResumeFrame.ResumeToken)));
        var receiveCursor = new Sequence(ReadUInt64(ref reader, nameof(ReliableSessionResumeFrame.ReceiveCursor)));
        var committedCursor = new Sequence(ReadUInt64(ref reader, nameof(ReliableSessionResumeFrame.CommittedCursor)));
        var hasSnapshotVersion = (flags & FlagHasSnapshotVersion) != 0;
        var snapshotVersion = hasSnapshotVersion
            ? new SnapshotVersion(ReadInt64(ref reader, nameof(ReliableSessionResumeFrame.SnapshotVersion)))
            : new SnapshotVersion(-1);

        return new ReliableSessionResumeFrame
        {
            SessionId = sessionId,
            ConnectionId = connectionId,
            ClientInstanceId = clientInstanceId,
            ResumeToken = resumeToken,
            ReceiveCursor = receiveCursor,
            CommittedCursor = committedCursor,
            HasSnapshotVersion = hasSnapshotVersion,
            SnapshotVersion = snapshotVersion
        };
    }

    private static byte EncodeResumeAck(IBufferWriter<byte> writer, ReliableSessionResumeAckFrame frame)
    {
        ValidateSessionId(frame.SessionId);
        ValidateConnectionId(frame.ConnectionId);
        ValidateResumeToken(frame.ResumeToken);
        frame.NegotiatedOptions.Validate();

        WriteGuid(writer, frame.SessionId.Value);
        WriteGuid(writer, frame.ConnectionId.Value);
        WriteUInt64(writer, frame.ResumeGeneration.Value);
        WriteUInt64(writer, frame.ReplayStart.Value);

        byte flags = 0;
        if (frame.SnapshotRequired)
            flags |= FlagSnapshotRequired;

        if (frame.RotateResumeToken)
            flags |= FlagRotateResumeToken;

        WriteString(writer, frame.ResumeToken.Value);
        EncodeHandshakeOptions(writer, frame.NegotiatedOptions);
        return flags;
    }

    private static ReliableSessionResumeAckFrame DecodeResumeAck(ref SequenceReader<byte> reader, byte flags)
    {
        var sessionId = new SessionId(ReadGuid(ref reader, nameof(ReliableSessionResumeAckFrame.SessionId)));
        var connectionId = new ConnectionId(ReadGuid(ref reader, nameof(ReliableSessionResumeAckFrame.ConnectionId)));
        var resumeGeneration = new ResumeGeneration(ReadUInt64(ref reader, nameof(ReliableSessionResumeAckFrame.ResumeGeneration)));
        var replayStart = new Sequence(ReadUInt64(ref reader, nameof(ReliableSessionResumeAckFrame.ReplayStart)));
        var resumeToken = new ResumeToken(ReadString(ref reader, nameof(ReliableSessionResumeAckFrame.ResumeToken)));
        var options = DecodeHandshakeOptions(ref reader);

        return new ReliableSessionResumeAckFrame
        {
            SessionId = sessionId,
            ConnectionId = connectionId,
            ResumeGeneration = resumeGeneration,
            ReplayStart = replayStart,
            SnapshotRequired = (flags & FlagSnapshotRequired) != 0,
            RotateResumeToken = (flags & FlagRotateResumeToken) != 0,
            ResumeToken = resumeToken,
            NegotiatedOptions = options
        };
    }

    private static void EncodeHeartbeat(IBufferWriter<byte> writer, ReliableSessionHeartbeatFrame frame)
    {
        ValidateSessionId(frame.SessionId);
        ValidateConnectionId(frame.ConnectionId);
        WriteGuid(writer, frame.SessionId.Value);
        WriteGuid(writer, frame.ConnectionId.Value);
        WriteUInt64(writer, frame.LastSentSequence.Value);
        WriteUInt64(writer, frame.LastAckedSequence.Value);
    }

    private static ReliableSessionHeartbeatFrame DecodeHeartbeat(ref SequenceReader<byte> reader)
    {
        return new ReliableSessionHeartbeatFrame
        {
            SessionId = new SessionId(ReadGuid(ref reader, nameof(ReliableSessionHeartbeatFrame.SessionId))),
            ConnectionId = new ConnectionId(ReadGuid(ref reader, nameof(ReliableSessionHeartbeatFrame.ConnectionId))),
            LastSentSequence = new Sequence(ReadUInt64(ref reader, nameof(ReliableSessionHeartbeatFrame.LastSentSequence))),
            LastAckedSequence = new Sequence(ReadUInt64(ref reader, nameof(ReliableSessionHeartbeatFrame.LastAckedSequence)))
        };
    }

    private static byte EncodeData(IBufferWriter<byte> writer, ReliableSessionDataFrame frame)
    {
        ValidateSessionId(frame.SessionId);
        WriteGuid(writer, frame.SessionId.Value);
        WriteUInt64(writer, frame.MessageId.Value);
        WriteUInt64(writer, frame.Sequence.Value);

        byte flags = 0;
        if (frame.HasSnapshotVersion)
        {
            flags |= FlagHasSnapshotVersion;
            WriteInt64(writer, frame.SnapshotVersion.Value);
        }

        var payload = frame.Payload ?? Array.Empty<byte>();
        WriteInt32(writer, payload.Length);
        WriteBytes(writer, payload);
        return flags;
    }

    private static ReliableSessionDataFrame DecodeData(ref SequenceReader<byte> reader, byte flags)
    {
        var sessionId = new SessionId(ReadGuid(ref reader, nameof(ReliableSessionDataFrame.SessionId)));
        var messageId = new MessageId(ReadUInt64(ref reader, nameof(ReliableSessionDataFrame.MessageId)));
        var sequence = new Sequence(ReadUInt64(ref reader, nameof(ReliableSessionDataFrame.Sequence)));
        var hasSnapshotVersion = (flags & FlagHasSnapshotVersion) != 0;
        var snapshotVersion = hasSnapshotVersion
            ? new SnapshotVersion(ReadInt64(ref reader, nameof(ReliableSessionDataFrame.SnapshotVersion)))
            : new SnapshotVersion(-1);
        var payloadLength = ReadInt32(ref reader, nameof(ReliableSessionDataFrame.Payload));
        var payload = ReadBytes(ref reader, payloadLength, nameof(ReliableSessionDataFrame.Payload));

        return new ReliableSessionDataFrame
        {
            SessionId = sessionId,
            MessageId = messageId,
            Sequence = sequence,
            HasSnapshotVersion = hasSnapshotVersion,
            SnapshotVersion = snapshotVersion,
            Payload = payload
        };
    }

    private static void EncodeAck(IBufferWriter<byte> writer, ReliableSessionAckFrame frame)
    {
        ValidateSessionId(frame.SessionId);
        ValidateAckRanges(frame.Ranges);

        WriteGuid(writer, frame.SessionId.Value);
        WriteInt32(writer, frame.Ranges.Length);
        foreach (var range in frame.Ranges)
        {
            WriteUInt64(writer, range.Start.Value);
            WriteUInt64(writer, range.End.Value);
        }
    }

    private static ReliableSessionAckFrame DecodeAck(ref SequenceReader<byte> reader)
    {
        var sessionId = new SessionId(ReadGuid(ref reader, nameof(ReliableSessionAckFrame.SessionId)));
        var count = ReadInt32(ref reader, nameof(ReliableSessionAckFrame.Ranges));
        if (count <= 0)
            throw new ReliableSessionProtocolException("The ReliableSession ack frame must contain at least one range.");

        var ranges = new AckRange[count];
        for (var i = 0; i < count; i++)
        {
            var start = new Sequence(ReadUInt64(ref reader, nameof(AckRange.Start)));
            var end = new Sequence(ReadUInt64(ref reader, nameof(AckRange.End)));
            try
            {
                ranges[i] = new AckRange(start, end);
            }
            catch (ArgumentException exception)
            {
                throw new ReliableSessionProtocolException("The ack range is invalid.", exception);
            }
        }

        ValidateAckRanges(ranges);
        return new ReliableSessionAckFrame
        {
            SessionId = sessionId,
            Ranges = ranges
        };
    }

    private static byte EncodeSnapshotRequest(IBufferWriter<byte> writer, ReliableSessionSnapshotRequestFrame frame)
    {
        ValidateSessionId(frame.SessionId);
        WriteGuid(writer, frame.SessionId.Value);
        WriteUInt64(writer, frame.FromSequence.Value);
        WriteInt64(writer, frame.SnapshotVersion.Value);

        byte flags = 0;
        if (frame.ReplayWindowInsufficient)
            flags |= FlagReplayWindowInsufficient;

        return flags;
    }

    private static ReliableSessionSnapshotRequestFrame DecodeSnapshotRequest(ref SequenceReader<byte> reader, byte flags)
    {
        return new ReliableSessionSnapshotRequestFrame
        {
            SessionId = new SessionId(ReadGuid(ref reader, nameof(ReliableSessionSnapshotRequestFrame.SessionId))),
            FromSequence = new Sequence(ReadUInt64(ref reader, nameof(ReliableSessionSnapshotRequestFrame.FromSequence))),
            SnapshotVersion = new SnapshotVersion(ReadInt64(ref reader, nameof(ReliableSessionSnapshotRequestFrame.SnapshotVersion))),
            ReplayWindowInsufficient = (flags & FlagReplayWindowInsufficient) != 0
        };
    }

    private static void EncodeSnapshot(IBufferWriter<byte> writer, ReliableSessionSnapshotFrame frame)
    {
        ValidateSessionId(frame.SessionId);
        WriteGuid(writer, frame.SessionId.Value);
        WriteInt64(writer, frame.SnapshotVersion.Value);
        WriteUInt64(writer, frame.BaseSequence.Value);
        var payload = frame.Payload ?? Array.Empty<byte>();
        WriteInt32(writer, payload.Length);
        WriteBytes(writer, payload);
    }

    private static ReliableSessionSnapshotFrame DecodeSnapshot(ref SequenceReader<byte> reader)
    {
        var sessionId = new SessionId(ReadGuid(ref reader, nameof(ReliableSessionSnapshotFrame.SessionId)));
        var snapshotVersion = new SnapshotVersion(ReadInt64(ref reader, nameof(ReliableSessionSnapshotFrame.SnapshotVersion)));
        var baseSequence = new Sequence(ReadUInt64(ref reader, nameof(ReliableSessionSnapshotFrame.BaseSequence)));
        var payloadLength = ReadInt32(ref reader, nameof(ReliableSessionSnapshotFrame.Payload));
        var payload = ReadBytes(ref reader, payloadLength, nameof(ReliableSessionSnapshotFrame.Payload));

        return new ReliableSessionSnapshotFrame
        {
            SessionId = sessionId,
            SnapshotVersion = snapshotVersion,
            BaseSequence = baseSequence,
            Payload = payload
        };
    }

    private static byte EncodeClose(IBufferWriter<byte> writer, ReliableSessionCloseFrame frame)
    {
        ValidateSessionId(frame.SessionId);
        WriteGuid(writer, frame.SessionId.Value);
        WriteByte(writer, (byte)frame.CloseReason);
        WriteString(writer, frame.Message ?? string.Empty);

        byte flags = 0;
        if (frame.HasErrorCode)
        {
            flags |= FlagHasErrorCode;
            WriteByte(writer, (byte)frame.ErrorCode);
        }

        return flags;
    }

    private static ReliableSessionCloseFrame DecodeClose(ref SequenceReader<byte> reader, byte flags)
    {
        var sessionId = new SessionId(ReadGuid(ref reader, nameof(ReliableSessionCloseFrame.SessionId)));
        var closeReason = (ReliableSessionCloseReason)ReadByte(ref reader, nameof(ReliableSessionCloseFrame.CloseReason));
        var message = ReadString(ref reader, nameof(ReliableSessionCloseFrame.Message));
        var hasErrorCode = (flags & FlagHasErrorCode) != 0;
        var errorCode = hasErrorCode
            ? (ReliableSessionErrorCode)ReadByte(ref reader, nameof(ReliableSessionCloseFrame.ErrorCode))
            : default;

        return new ReliableSessionCloseFrame
        {
            SessionId = sessionId,
            CloseReason = closeReason,
            HasErrorCode = hasErrorCode,
            ErrorCode = errorCode,
            Message = message
        };
    }

    private static byte EncodeError(IBufferWriter<byte> writer, ReliableSessionErrorFrame frame)
    {
        ValidateSessionId(frame.SessionId);
        WriteGuid(writer, frame.SessionId.Value);
        WriteByte(writer, (byte)frame.ErrorCode);
        WriteString(writer, frame.Message ?? string.Empty);

        byte flags = 0;
        if (frame.HasCloseReason)
        {
            flags |= FlagHasCloseReason;
            WriteByte(writer, (byte)frame.CloseReason);
        }

        return flags;
    }

    private static ReliableSessionErrorFrame DecodeError(ref SequenceReader<byte> reader, byte flags)
    {
        var sessionId = new SessionId(ReadGuid(ref reader, nameof(ReliableSessionErrorFrame.SessionId)));
        var errorCode = (ReliableSessionErrorCode)ReadByte(ref reader, nameof(ReliableSessionErrorFrame.ErrorCode));
        var message = ReadString(ref reader, nameof(ReliableSessionErrorFrame.Message));
        var hasCloseReason = (flags & FlagHasCloseReason) != 0;
        var closeReason = hasCloseReason
            ? (ReliableSessionCloseReason)ReadByte(ref reader, nameof(ReliableSessionErrorFrame.CloseReason))
            : default;

        return new ReliableSessionErrorFrame
        {
            SessionId = sessionId,
            ErrorCode = errorCode,
            HasCloseReason = hasCloseReason,
            CloseReason = closeReason,
            Message = message
        };
    }

    private static void EncodeHandshakeOptions(IBufferWriter<byte> writer, ReliableSessionHandshakeOptions options)
    {
        options.Validate();
        WriteInt64(writer, options.HeartbeatInterval.Ticks);
        WriteInt64(writer, options.HeartbeatTimeout.Ticks);
        WriteInt32(writer, options.HeartbeatMissThreshold);
        WriteInt64(writer, options.DisconnectGracePeriod.Ticks);
        WriteInt64(writer, options.RecoveryWindow.Ticks);
        WriteInt32(writer, options.ReplayWindowSize);
        WriteInt64(writer, options.ReplayWindowBytes);
        WriteInt32(writer, options.DedupWindowSize);
        WriteInt32(writer, options.MaxReplayFrames);
        WriteInt32(writer, options.MaxBufferedOutOfOrderFrames);
        WriteInt64(writer, options.ResumeTokenLifetime.Ticks);
        WriteByte(writer, options.RotateResumeToken ? (byte)1 : (byte)0);
        WriteInt64(writer, options.SnapshotRequiredAfter.Ticks);
    }

    private static ReliableSessionHandshakeOptions DecodeHandshakeOptions(ref SequenceReader<byte> reader)
    {
        return new ReliableSessionHandshakeOptions
        {
            HeartbeatInterval = TimeSpan.FromTicks(ReadInt64(ref reader, nameof(ReliableSessionHandshakeOptions.HeartbeatInterval))),
            HeartbeatTimeout = TimeSpan.FromTicks(ReadInt64(ref reader, nameof(ReliableSessionHandshakeOptions.HeartbeatTimeout))),
            HeartbeatMissThreshold = ReadInt32(ref reader, nameof(ReliableSessionHandshakeOptions.HeartbeatMissThreshold)),
            DisconnectGracePeriod = TimeSpan.FromTicks(ReadInt64(ref reader, nameof(ReliableSessionHandshakeOptions.DisconnectGracePeriod))),
            RecoveryWindow = TimeSpan.FromTicks(ReadInt64(ref reader, nameof(ReliableSessionHandshakeOptions.RecoveryWindow))),
            ReplayWindowSize = ReadInt32(ref reader, nameof(ReliableSessionHandshakeOptions.ReplayWindowSize)),
            ReplayWindowBytes = ReadInt64(ref reader, nameof(ReliableSessionHandshakeOptions.ReplayWindowBytes)),
            DedupWindowSize = ReadInt32(ref reader, nameof(ReliableSessionHandshakeOptions.DedupWindowSize)),
            MaxReplayFrames = ReadInt32(ref reader, nameof(ReliableSessionHandshakeOptions.MaxReplayFrames)),
            MaxBufferedOutOfOrderFrames = ReadInt32(ref reader, nameof(ReliableSessionHandshakeOptions.MaxBufferedOutOfOrderFrames)),
            ResumeTokenLifetime = TimeSpan.FromTicks(ReadInt64(ref reader, nameof(ReliableSessionHandshakeOptions.ResumeTokenLifetime))),
            RotateResumeToken = ReadByte(ref reader, nameof(ReliableSessionHandshakeOptions.RotateResumeToken)) != 0,
            SnapshotRequiredAfter = TimeSpan.FromTicks(ReadInt64(ref reader, nameof(ReliableSessionHandshakeOptions.SnapshotRequiredAfter)))
        };
    }

    private static void ValidateFrame(ReliableSessionFrame frame)
    {
        switch (frame)
        {
            case ReliableSessionHelloFrame hello:
                if (hello.RequestedOptions == null)
                    throw new ReliableSessionProtocolException("The requested options cannot be null.");

                if (hello.ClientInstanceId.IsEmpty)
                    throw new ReliableSessionProtocolException("The client instance identifier cannot be empty.");

                if (hello.ProtocolVersion == 0)
                    throw new ReliableSessionProtocolException("The protocol version must be greater than zero.");

                hello.RequestedOptions.Validate();
                break;
            case ReliableSessionHelloAckFrame helloAck:
                ValidateSessionId(helloAck.SessionId);
                ValidateConnectionId(helloAck.ConnectionId);
                ValidateResumeToken(helloAck.ResumeToken);
                if (helloAck.NegotiatedOptions == null)
                    throw new ReliableSessionProtocolException("The negotiated options cannot be null.");

                helloAck.NegotiatedOptions.Validate();
                break;
            case ReliableSessionResumeFrame resume:
                ValidateSessionId(resume.SessionId);
                ValidateConnectionId(resume.ConnectionId);
                ValidateClientInstanceId(resume.ClientInstanceId);
                ValidateResumeToken(resume.ResumeToken);
                if (resume.HasSnapshotVersion && !resume.SnapshotVersion.HasValue)
                    throw new ReliableSessionProtocolException("The snapshot version is marked as present but is empty.");

                if (!resume.HasSnapshotVersion && resume.SnapshotVersion.HasValue)
                    throw new ReliableSessionProtocolException("The snapshot version is marked as absent but still contains a value.");
                break;
            case ReliableSessionResumeAckFrame resumeAck:
                ValidateSessionId(resumeAck.SessionId);
                ValidateConnectionId(resumeAck.ConnectionId);
                ValidateResumeToken(resumeAck.ResumeToken);
                if (resumeAck.NegotiatedOptions == null)
                    throw new ReliableSessionProtocolException("The negotiated options cannot be null.");

                resumeAck.NegotiatedOptions.Validate();
                break;
            case ReliableSessionHeartbeatFrame heartbeat:
                ValidateSessionId(heartbeat.SessionId);
                ValidateConnectionId(heartbeat.ConnectionId);
                break;
            case ReliableSessionDataFrame data:
                ValidateSessionId(data.SessionId);
                if (data.HasSnapshotVersion && !data.SnapshotVersion.HasValue)
                    throw new ReliableSessionProtocolException("The data frame snapshot version is marked as present but is empty.");

                if (!data.HasSnapshotVersion && data.SnapshotVersion.HasValue)
                    throw new ReliableSessionProtocolException("The data frame snapshot version is marked as absent but still contains a value.");
                break;
            case ReliableSessionAckFrame ack:
                ValidateSessionId(ack.SessionId);
                ValidateAckRanges(ack.Ranges);
                break;
            case ReliableSessionSnapshotRequestFrame snapshotRequest:
                ValidateSessionId(snapshotRequest.SessionId);
                break;
            case ReliableSessionSnapshotFrame snapshot:
                ValidateSessionId(snapshot.SessionId);
                break;
            case ReliableSessionCloseFrame close:
                ValidateSessionId(close.SessionId);
                break;
            case ReliableSessionErrorFrame error:
                ValidateSessionId(error.SessionId);
                break;
        }
    }

    private static void ValidateSessionId(SessionId sessionId)
    {
        if (sessionId.IsEmpty)
            throw new ReliableSessionProtocolException("The session identifier cannot be empty.");
    }

    private static void ValidateConnectionId(ConnectionId connectionId)
    {
        if (connectionId.IsEmpty)
            throw new ReliableSessionProtocolException("The connection identifier cannot be empty.");
    }

    private static void ValidateClientInstanceId(ClientInstanceId clientInstanceId)
    {
        if (clientInstanceId.IsEmpty)
            throw new ReliableSessionProtocolException("The client instance identifier cannot be empty.");
    }

    private static void ValidateResumeToken(ResumeToken resumeToken)
    {
        if (resumeToken.IsEmpty)
            throw new ReliableSessionProtocolException("The resume token cannot be empty.");
    }

    private static void ValidateAckRanges(AckRange[] ranges)
    {
        if (ranges == null || ranges.Length == 0)
            throw new ReliableSessionProtocolException("The ack frame must contain at least one range.");

        ulong previousEnd = 0;
        var hasPrevious = false;

        for (var i = 0; i < ranges.Length; i++)
        {
            var range = ranges[i];
            if (range.Start.Value > range.End.Value)
                throw new ReliableSessionProtocolException("The ack range start cannot be greater than the end.");

            if (hasPrevious && range.Start.Value <= previousEnd)
                throw new ReliableSessionProtocolException("The ack ranges must be sorted and non-overlapping.");

            previousEnd = range.End.Value;
            hasPrevious = true;
        }
    }

    private static byte ReadByte(ref SequenceReader<byte> reader, string fieldName)
    {
        if (!reader.TryRead(out var value))
            throw new ReliableSessionProtocolException($"The field '{fieldName}' is truncated.");

        return value;
    }

    private static ushort ReadUInt16(ref SequenceReader<byte> reader, string fieldName)
    {
        if (!reader.TryReadLittleEndian(out ushort value))
            throw new ReliableSessionProtocolException($"The field '{fieldName}' is truncated.");

        return value;
    }

    private static int ReadInt32(ref SequenceReader<byte> reader, string fieldName)
    {
        if (!reader.TryReadLittleEndian(out uint value))
            throw new ReliableSessionProtocolException($"The field '{fieldName}' is truncated.");

        if (value > int.MaxValue)
            throw new ReliableSessionProtocolException($"The field '{fieldName}' is out of range.");

        return (int)value;
    }

    private static long ReadInt64(ref SequenceReader<byte> reader, string fieldName)
    {
        if (!reader.TryReadLittleEndian(out ulong value))
            throw new ReliableSessionProtocolException($"The field '{fieldName}' is truncated.");

        if (value > long.MaxValue)
            throw new ReliableSessionProtocolException($"The field '{fieldName}' is out of range.");

        return (long)value;
    }

    private static ulong ReadUInt64(ref SequenceReader<byte> reader, string fieldName)
    {
        if (!reader.TryReadLittleEndian(out ulong value))
            throw new ReliableSessionProtocolException($"The field '{fieldName}' is truncated.");

        return value;
    }

    private static Guid ReadGuid(ref SequenceReader<byte> reader, string fieldName)
    {
        Span<byte> bytes = stackalloc byte[16];
        if (!reader.TryCopyTo(bytes))
            throw new ReliableSessionProtocolException($"The field '{fieldName}' is truncated.");

        reader.Advance(16);
        return new Guid(bytes);
    }

    private static string ReadString(ref SequenceReader<byte> reader, string fieldName)
    {
        var length = ReadInt32(ref reader, fieldName);
        if (length < 0)
            throw new ReliableSessionProtocolException($"The field '{fieldName}' has an invalid length.");

        var bytes = ReadBytes(ref reader, length, fieldName);
        return Encoding.UTF8.GetString(bytes);
    }

    private static byte[] ReadBytes(ref SequenceReader<byte> reader, int length, string fieldName)
    {
        if (length < 0)
            throw new ReliableSessionProtocolException($"The field '{fieldName}' has an invalid length.");

        if (reader.Remaining < length)
            throw new ReliableSessionProtocolException($"The field '{fieldName}' is truncated.");

        var bytes = reader.Sequence.Slice(reader.Consumed, length).ToArray();
        reader.Advance(length);
        return bytes;
    }

    private static void WriteByte(IBufferWriter<byte> writer, byte value)
    {
        var span = writer.GetSpan(1);
        span[0] = value;
        writer.Advance(1);
    }

    private static void WriteUInt16(IBufferWriter<byte> writer, ushort value)
    {
        var span = writer.GetSpan(sizeof(ushort));
        BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        writer.Advance(sizeof(ushort));
    }

    private static void WriteInt32(IBufferWriter<byte> writer, int value)
    {
        var span = writer.GetSpan(sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(span, value);
        writer.Advance(sizeof(int));
    }

    private static void WriteInt64(IBufferWriter<byte> writer, long value)
    {
        var span = writer.GetSpan(sizeof(long));
        BinaryPrimitives.WriteInt64LittleEndian(span, value);
        writer.Advance(sizeof(long));
    }

    private static void WriteUInt64(IBufferWriter<byte> writer, ulong value)
    {
        var span = writer.GetSpan(sizeof(ulong));
        BinaryPrimitives.WriteUInt64LittleEndian(span, value);
        writer.Advance(sizeof(ulong));
    }

    private static void WriteGuid(IBufferWriter<byte> writer, Guid value)
    {
        var span = writer.GetSpan(16);
        if (!value.TryWriteBytes(span))
            throw new ReliableSessionProtocolException("Failed to serialize a Guid value.");

        writer.Advance(16);
    }

    private static void WriteString(IBufferWriter<byte> writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        WriteInt32(writer, bytes.Length);
        WriteBytes(writer, bytes);
    }

    private static void WriteBytes(IBufferWriter<byte> writer, ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
            return;

        var span = writer.GetSpan(bytes.Length);
        bytes.CopyTo(span);
        writer.Advance(bytes.Length);
    }
}
