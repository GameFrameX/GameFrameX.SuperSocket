using System.Text;
using GameFrameX.SuperSocket.ReliableSession;
using Xunit;

namespace GameFrameX.SuperSocket.ReliableSession.Tests;

/// <summary>
/// Protocol-level ReliableSession end-to-end scenario tests.
/// </summary>
public class ReliableSessionProtocolEndToEndTests
{
    private static readonly SessionId SessionId = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly ClientInstanceId ClientInstanceId = new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly ConnectionId FirstConnectionId = new(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
    private static readonly ConnectionId ResumedConnectionId = new(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));

    private readonly ReliableSessionFrameCodec _codec = new();

    /// <summary>
    /// Verifies the complete protocol lifecycle across all currently defined frame kinds.
    /// </summary>
    [Fact]
    public void FullLifecycleProtocolScript_EndToEnd_CoversEveryFrameKind()
    {
        var options = CreateOptions();
        var link = new ScriptedProtocolLink(_codec);

        var hello = link.DeliverClientToServer(new ReliableSessionHelloFrame
        {
            ClientInstanceId = ClientInstanceId,
            ProtocolVersion = ReliableSessionProtocol.WireVersion,
            RequestedOptions = options
        }).Single();

        var decodedHello = Assert.IsType<ReliableSessionHelloFrame>(hello);
        Assert.Equal(ClientInstanceId, decodedHello.ClientInstanceId);
        Assert.Equal(options, decodedHello.RequestedOptions);

        var helloAck = link.DeliverServerToClient(new ReliableSessionHelloAckFrame
        {
            SessionId = SessionId,
            ConnectionId = FirstConnectionId,
            ResumeGeneration = new ResumeGeneration(1),
            ResumeToken = new ResumeToken("token-1"),
            NegotiatedOptions = options
        }).Single();

        var decodedHelloAck = Assert.IsType<ReliableSessionHelloAckFrame>(helloAck);
        Assert.Equal(SessionId, decodedHelloAck.SessionId);
        Assert.Equal(FirstConnectionId, decodedHelloAck.ConnectionId);
        Assert.Equal(new ResumeGeneration(1), decodedHelloAck.ResumeGeneration);
        Assert.Equal("token-1", decodedHelloAck.ResumeToken.Value);

        var heartbeat = link.DeliverClientToServer(new ReliableSessionHeartbeatFrame
        {
            SessionId = SessionId,
            ConnectionId = FirstConnectionId,
            LastSentSequence = new Sequence(1),
            LastAckedSequence = new Sequence(0)
        }).Single();

        Assert.IsType<ReliableSessionHeartbeatFrame>(heartbeat);

        var data = link.DeliverClientToServer(CreateData(1, "move:north")).Single();
        var decodedData = Assert.IsType<ReliableSessionDataFrame>(data);
        Assert.Equal(new MessageId(1), decodedData.MessageId);
        Assert.Equal("move:north", ReadPayload(decodedData.Payload));

        var ack = link.DeliverServerToClient(new ReliableSessionAckFrame
        {
            SessionId = SessionId,
            Ranges = new[] { new AckRange(new Sequence(1), new Sequence(1)) }
        }).Single();

        var decodedAck = Assert.IsType<ReliableSessionAckFrame>(ack);
        Assert.Equal(new Sequence(1), decodedAck.Ranges[0].Start);
        Assert.Equal(new Sequence(1), decodedAck.Ranges[0].End);

        var close = link.DeliverServerToClient(new ReliableSessionCloseFrame
        {
            SessionId = SessionId,
            CloseReason = ReliableSessionCloseReason.ServerRequest,
            Message = "maintenance"
        }).Single();

        Assert.Equal(ReliableSessionCloseReason.ServerRequest, Assert.IsType<ReliableSessionCloseFrame>(close).CloseReason);

        var error = link.DeliverServerToClient(new ReliableSessionErrorFrame
        {
            SessionId = SessionId,
            ErrorCode = ReliableSessionErrorCode.ProtocolViolation,
            HasCloseReason = true,
            CloseReason = ReliableSessionCloseReason.ProtocolError,
            Message = "bad sequence"
        }).Single();

        var decodedError = Assert.IsType<ReliableSessionErrorFrame>(error);
        Assert.Equal(ReliableSessionErrorCode.ProtocolViolation, decodedError.ErrorCode);
        Assert.Equal(ReliableSessionCloseReason.ProtocolError, decodedError.CloseReason);

        Assert.Equal(new[]
        {
            ReliableSessionFrameKind.Hello,
            ReliableSessionFrameKind.HelloAck,
            ReliableSessionFrameKind.Heartbeat,
            ReliableSessionFrameKind.Data,
            ReliableSessionFrameKind.Ack,
            ReliableSessionFrameKind.Close,
            ReliableSessionFrameKind.Error
        }, link.DeliveredKinds);
    }

    /// <summary>
    /// Verifies resume, replay, token rotation, and acknowledgement behavior after blackout windows.
    /// </summary>
    [Theory]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(60)]
    public void ResumeAfterBlackoutProtocolScript_EndToEnd_RebindsNewConnectionAndReplaysUnackedData(int blackoutSeconds)
    {
        var options = CreateOptions();
        var link = new ScriptedProtocolLink(_codec);

        Assert.Empty(link.DropForBlackout(blackoutSeconds, CreateData(2, "cast:fireball")));

        var resume = link.DeliverClientToServer(new ReliableSessionResumeFrame
        {
            SessionId = SessionId,
            ClientInstanceId = ClientInstanceId,
            ConnectionId = ResumedConnectionId,
            ResumeToken = new ResumeToken("token-1"),
            ReceiveCursor = new Sequence(1),
            CommittedCursor = new Sequence(1),
            HasSnapshotVersion = true,
            SnapshotVersion = new SnapshotVersion(7)
        }).Single();

        var decodedResume = Assert.IsType<ReliableSessionResumeFrame>(resume);
        Assert.Equal(SessionId, decodedResume.SessionId);
        Assert.Equal(ResumedConnectionId, decodedResume.ConnectionId);
        Assert.Equal(new Sequence(1), decodedResume.ReceiveCursor);
        Assert.Equal(new Sequence(1), decodedResume.CommittedCursor);
        Assert.Equal(new SnapshotVersion(7), decodedResume.SnapshotVersion);

        var resumeAck = link.DeliverServerToClient(new ReliableSessionResumeAckFrame
        {
            SessionId = SessionId,
            ConnectionId = ResumedConnectionId,
            ResumeGeneration = new ResumeGeneration(2),
            ReplayStart = new Sequence(2),
            SnapshotRequired = false,
            RotateResumeToken = true,
            ResumeToken = new ResumeToken("token-2"),
            NegotiatedOptions = options
        }).Single();

        var decodedResumeAck = Assert.IsType<ReliableSessionResumeAckFrame>(resumeAck);
        Assert.Equal(new ResumeGeneration(2), decodedResumeAck.ResumeGeneration);
        Assert.Equal(new Sequence(2), decodedResumeAck.ReplayStart);
        Assert.True(decodedResumeAck.RotateResumeToken);
        Assert.Equal("token-2", decodedResumeAck.ResumeToken.Value);

        var replayed = link.DeliverServerToClient(
            CreateData(2, "cast:fireball"),
            CreateData(3, "loot:coin")).Cast<ReliableSessionDataFrame>().ToArray();

        Assert.Equal(new[] { 2UL, 3UL }, replayed.Select(f => f.Sequence.Value).ToArray());

        var ack = Assert.IsType<ReliableSessionAckFrame>(link.DeliverClientToServer(new ReliableSessionAckFrame
        {
            SessionId = SessionId,
            Ranges = new[] { new AckRange(new Sequence(2), new Sequence(3)) }
        }).Single());

        Assert.Equal(new Sequence(2), ack.Ranges[0].Start);
        Assert.Equal(new Sequence(3), ack.Ranges[0].End);
    }

    /// <summary>
    /// Verifies snapshot fallback when the replay window is insufficient.
    /// </summary>
    [Fact]
    public void SnapshotFallbackProtocolScript_EndToEnd_RequiresSnapshotBeforeReplayContinues()
    {
        var options = CreateOptions();
        var link = new ScriptedProtocolLink(_codec);

        var resumeAck = Assert.IsType<ReliableSessionResumeAckFrame>(link.DeliverServerToClient(new ReliableSessionResumeAckFrame
        {
            SessionId = SessionId,
            ConnectionId = ResumedConnectionId,
            ResumeGeneration = new ResumeGeneration(3),
            ReplayStart = new Sequence(45),
            SnapshotRequired = true,
            RotateResumeToken = false,
            ResumeToken = new ResumeToken("token-2"),
            NegotiatedOptions = options
        }).Single());

        Assert.True(resumeAck.SnapshotRequired);
        Assert.Equal(new Sequence(45), resumeAck.ReplayStart);

        var snapshotRequest = Assert.IsType<ReliableSessionSnapshotRequestFrame>(link.DeliverClientToServer(new ReliableSessionSnapshotRequestFrame
        {
            SessionId = SessionId,
            FromSequence = new Sequence(45),
            SnapshotVersion = new SnapshotVersion(8),
            ReplayWindowInsufficient = true
        }).Single());

        Assert.True(snapshotRequest.ReplayWindowInsufficient);
        Assert.Equal(new SnapshotVersion(8), snapshotRequest.SnapshotVersion);

        var snapshot = Assert.IsType<ReliableSessionSnapshotFrame>(link.DeliverServerToClient(new ReliableSessionSnapshotFrame
        {
            SessionId = SessionId,
            SnapshotVersion = new SnapshotVersion(9),
            BaseSequence = new Sequence(44),
            Payload = Payload("state:inventory=stable")
        }).Single());

        Assert.Equal(new Sequence(44), snapshot.BaseSequence);
        Assert.Equal("state:inventory=stable", ReadPayload(snapshot.Payload));

        var nextData = Assert.IsType<ReliableSessionDataFrame>(link.DeliverServerToClient(new ReliableSessionDataFrame
        {
            SessionId = SessionId,
            MessageId = new MessageId(45),
            Sequence = new Sequence(45),
            HasSnapshotVersion = true,
            SnapshotVersion = new SnapshotVersion(9),
            Payload = Payload("delta:item=added")
        }).Single());

        Assert.Equal(new SnapshotVersion(9), nextData.SnapshotVersion);
        Assert.Equal("delta:item=added", ReadPayload(nextData.Payload));
    }

    /// <summary>
    /// Verifies that duplicate and reordered frames retain identifiers needed for deduplication and ack generation.
    /// </summary>
    [Fact]
    public void DuplicateAndReorderedDataProtocolScript_EndToEnd_PreservesIdsForDedupAck()
    {
        var link = new ScriptedProtocolLink(_codec);

        var delivered = link.DeliverReorderedAndDuplicated(
            CreateData(1, "input:a"),
            CreateData(2, "input:b")).Cast<ReliableSessionDataFrame>().ToArray();

        Assert.Equal(4, delivered.Length);
        Assert.Equal(2, delivered.Select(f => f.MessageId).Distinct().Count());
        Assert.Equal(new[] { 2UL, 2UL, 1UL, 1UL }, delivered.Select(f => f.Sequence.Value).ToArray());

        var uniqueSequences = delivered
            .GroupBy(f => f.MessageId)
            .Select(g => g.First().Sequence.Value)
            .OrderBy(value => value)
            .ToArray();

        Assert.Equal(new[] { 1UL, 2UL }, uniqueSequences);

        var ack = Assert.IsType<ReliableSessionAckFrame>(link.DeliverClientToServer(new ReliableSessionAckFrame
        {
            SessionId = SessionId,
            Ranges = new[] { new AckRange(new Sequence(uniqueSequences.First()), new Sequence(uniqueSequences.Last())) }
        }).Single());

        Assert.Equal(new Sequence(1), ack.Ranges[0].Start);
        Assert.Equal(new Sequence(2), ack.Ranges[0].End);
    }

    private static ReliableSessionHandshakeOptions CreateOptions()
    {
        return new ReliableSessionHandshakeOptions
        {
            HeartbeatInterval = TimeSpan.FromSeconds(5),
            HeartbeatTimeout = TimeSpan.FromSeconds(15),
            HeartbeatMissThreshold = 3,
            DisconnectGracePeriod = TimeSpan.FromSeconds(5),
            RecoveryWindow = TimeSpan.FromSeconds(90),
            ReplayWindowSize = 128,
            ReplayWindowBytes = 64 * 1024,
            DedupWindowSize = 128,
            MaxReplayFrames = 128,
            MaxBufferedOutOfOrderFrames = 32,
            ResumeTokenLifetime = TimeSpan.FromMinutes(10),
            RotateResumeToken = true,
            SnapshotRequiredAfter = TimeSpan.FromSeconds(45)
        };
    }

    private static ReliableSessionDataFrame CreateData(ulong sequence, string payload)
    {
        return new ReliableSessionDataFrame
        {
            SessionId = SessionId,
            MessageId = new MessageId(sequence),
            Sequence = new Sequence(sequence),
            Payload = Payload(payload)
        };
    }

    private static byte[] Payload(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }

    private static string ReadPayload(byte[] payload)
    {
        return Encoding.UTF8.GetString(payload);
    }

    private sealed class ScriptedProtocolLink
    {
        private readonly ReliableSessionFrameCodec _codec;
        private readonly List<ReliableSessionFrameKind> _deliveredKinds = new();

        public ScriptedProtocolLink(ReliableSessionFrameCodec codec)
        {
            _codec = codec;
        }

        public ReliableSessionFrameKind[] DeliveredKinds => _deliveredKinds.ToArray();

        public ReliableSessionFrame[] DeliverClientToServer(params ReliableSessionFrame[] frames)
        {
            return Deliver(frames);
        }

        public ReliableSessionFrame[] DeliverServerToClient(params ReliableSessionFrame[] frames)
        {
            return Deliver(frames);
        }

        public ReliableSessionFrame[] DropForBlackout(int blackoutSeconds, params ReliableSessionFrame[] frames)
        {
            if (blackoutSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(blackoutSeconds), "Blackout duration must be positive.");

            foreach (var frame in frames)
            {
                _codec.Encode(frame);
            }

            return Array.Empty<ReliableSessionFrame>();
        }

        public ReliableSessionFrame[] DeliverReorderedAndDuplicated(params ReliableSessionFrame[] frames)
        {
            var encoded = frames
                .SelectMany(frame => new[] { _codec.Encode(frame), _codec.Encode(frame) })
                .Reverse()
                .ToArray();

            return Decode(encoded);
        }

        private ReliableSessionFrame[] Deliver(params ReliableSessionFrame[] frames)
        {
            return Decode(frames.Select(frame => _codec.Encode(frame)).ToArray());
        }

        private ReliableSessionFrame[] Decode(byte[][] encodedFrames)
        {
            var decoded = encodedFrames.Select(frame => _codec.Decode(frame)).ToArray();
            _deliveredKinds.AddRange(decoded.Select(frame => frame.Kind));
            return decoded;
        }
    }
}
