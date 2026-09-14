using System.Buffers.Binary;
using GameFrameX.SuperSocket.ReliableSession;
using Xunit;

namespace GameFrameX.SuperSocket.ReliableSession.Tests;

/// <summary>
/// ReliableSession frame codec regression tests.
/// </summary>
public class ReliableSessionFrameCodecTests
{
    private readonly ReliableSessionFrameCodec _codec = new();

    /// <summary>
    /// Verifies that a hello frame round-trips through the codec.
    /// </summary>
    [Fact]
    public void EncodeAndDecodeHelloFrame()
    {
        var frame = new ReliableSessionHelloFrame
        {
            ClientInstanceId = new ClientInstanceId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            ProtocolVersion = 3,
            RequestedOptions = new ReliableSessionHandshakeOptions
            {
                HeartbeatInterval = TimeSpan.FromSeconds(7),
                HeartbeatTimeout = TimeSpan.FromSeconds(21),
                HeartbeatMissThreshold = 4,
                DisconnectGracePeriod = TimeSpan.FromSeconds(6),
                RecoveryWindow = TimeSpan.FromMinutes(3),
                ReplayWindowSize = 2048,
                ReplayWindowBytes = 8 * 1024 * 1024,
                DedupWindowSize = 4096,
                MaxReplayFrames = 2048,
                MaxBufferedOutOfOrderFrames = 512,
                ResumeTokenLifetime = TimeSpan.FromMinutes(90),
                RotateResumeToken = false,
                SnapshotRequiredAfter = TimeSpan.FromMinutes(8)
            }
        };

        var decoded = _codec.Decode(_codec.Encode(frame));

        var hello = Assert.IsType<ReliableSessionHelloFrame>(decoded);
        Assert.Equal(frame.ClientInstanceId, hello.ClientInstanceId);
        Assert.Equal(frame.ProtocolVersion, hello.ProtocolVersion);
        Assert.Equal(frame.RequestedOptions, hello.RequestedOptions);
    }

    /// <summary>
    /// Verifies that a data frame with an empty payload round-trips through the codec.
    /// </summary>
    [Fact]
    public void EncodeAndDecodeDataFrameWithEmptyPayload()
    {
        var frame = new ReliableSessionDataFrame
        {
            SessionId = new SessionId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            MessageId = new MessageId(42),
            Sequence = new Sequence(99),
            HasSnapshotVersion = false,
            SnapshotVersion = new SnapshotVersion(-1),
            Payload = Array.Empty<byte>()
        };

        var decoded = _codec.Decode(_codec.Encode(frame));

        var data = Assert.IsType<ReliableSessionDataFrame>(decoded);
        Assert.Equal(frame.SessionId, data.SessionId);
        Assert.Equal(frame.MessageId, data.MessageId);
        Assert.Equal(frame.Sequence, data.Sequence);
        Assert.False(data.HasSnapshotVersion);
        Assert.False(data.SnapshotVersion.HasValue);
        Assert.Empty(data.Payload);
    }

    /// <summary>
    /// Verifies that acknowledgement ranges round-trip through the codec.
    /// </summary>
    [Fact]
    public void EncodeAndDecodeAckFrame()
    {
        var frame = new ReliableSessionAckFrame
        {
            SessionId = new SessionId(Guid.Parse("33333333-3333-3333-3333-333333333333")),
            Ranges = new[]
            {
                new AckRange(new Sequence(1), new Sequence(3)),
                new AckRange(new Sequence(8), new Sequence(10))
            }
        };

        var decoded = _codec.Decode(_codec.Encode(frame));

        var ack = Assert.IsType<ReliableSessionAckFrame>(decoded);
        Assert.Equal(frame.SessionId, ack.SessionId);
        Assert.Equal(frame.Ranges, ack.Ranges);
    }

    /// <summary>
    /// Verifies that unsupported wire versions are rejected.
    /// </summary>
    [Fact]
    public void DecodeRejectsUnsupportedVersion()
    {
        var bytes = _codec.Encode(new ReliableSessionHelloFrame
        {
            ClientInstanceId = new ClientInstanceId(Guid.Parse("44444444-4444-4444-4444-444444444444")),
            ProtocolVersion = 1
        });

        bytes[0] = 2;

        Assert.Throws<ReliableSessionProtocolException>(() => _codec.Decode(bytes));
    }

    /// <summary>
    /// Verifies that inverted acknowledgement ranges are rejected during decode.
    /// </summary>
    [Fact]
    public void DecodeRejectsInvalidAckRange()
    {
        var bytes = BuildInvalidAckFrame();

        Assert.Throws<ReliableSessionProtocolException>(() => _codec.Decode(bytes));
    }

    /// <summary>
    /// Verifies that required session and resume-token identifiers cannot be empty.
    /// </summary>
    [Fact]
    public void EncodeRejectsEmptySessionAndResumeToken()
    {
        var emptySessionFrame = new ReliableSessionHelloAckFrame
        {
            SessionId = new SessionId(Guid.Empty),
            ConnectionId = new ConnectionId(Guid.NewGuid()),
            ResumeGeneration = new ResumeGeneration(1),
            ResumeToken = new ResumeToken("token")
        };

        var emptyTokenFrame = new ReliableSessionResumeFrame
        {
            SessionId = new SessionId(Guid.NewGuid()),
            ClientInstanceId = new ClientInstanceId(Guid.NewGuid()),
            ConnectionId = new ConnectionId(Guid.NewGuid()),
            ResumeToken = new ResumeToken(string.Empty),
            ReceiveCursor = new Sequence(1),
            CommittedCursor = new Sequence(1)
        };

        Assert.Throws<ReliableSessionProtocolException>(() => _codec.Encode(emptySessionFrame));
        Assert.Throws<ReliableSessionProtocolException>(() => _codec.Encode(emptyTokenFrame));
    }

    private static byte[] BuildInvalidAckFrame()
    {
        var body = new byte[16 + 4 + 16];
        var span = body.AsSpan();
        var offset = 0;

        var sessionId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        sessionId.TryWriteBytes(span.Slice(offset, 16));
        offset += 16;

        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(offset, 4), 1);
        offset += 4;

        BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(offset, 8), 5);
        offset += 8;

        BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(offset, 8), 3);

        var output = new byte[ReliableSessionProtocol.FrameHeaderSize + body.Length];
        output[0] = ReliableSessionProtocol.WireVersion;
        output[1] = (byte)ReliableSessionFrameKind.Ack;
        BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(4, 4), body.Length);
        body.CopyTo(output.AsSpan(ReliableSessionProtocol.FrameHeaderSize));
        return output;
    }
}
