namespace GameFrameX.SuperSocket.ReliableSession;

/// <summary>
/// 表示 ReliableSession 协议帧的基类。
/// </summary>
/// <remarks>
/// Represents the base frame contract for the C3 protocol model; it does not implement runtime delivery, replay cache, or transport adapters.
/// </remarks>
public abstract record ReliableSessionFrame
{
    /// <summary>
    /// 获取协议帧类型。
    /// </summary>
    /// <remarks>
    /// Gets the frame kind used by the binary codec.
    /// </remarks>
    /// <value>协议帧类型 / Protocol frame kind</value>
    public abstract ReliableSessionFrameKind Kind { get; }
}

/// <summary>
/// 表示新建逻辑会话的握手请求帧。
/// </summary>
/// <remarks>
/// Represents a client hello frame that starts a logical ReliableSession and asks the server to negotiate heartbeat, replay, deduplication, and recovery options.
/// </remarks>
public sealed record ReliableSessionHelloFrame : ReliableSessionFrame
{
    /// <summary>
    /// 获取协议帧类型。
    /// </summary>
    /// <remarks>
    /// Gets the frame kind for a hello request.
    /// </remarks>
    /// <value>固定为 <see cref="ReliableSessionFrameKind.Hello"/> / Always <see cref="ReliableSessionFrameKind.Hello"/></value>
    public override ReliableSessionFrameKind Kind => ReliableSessionFrameKind.Hello;

    /// <summary>
    /// 获取或设置客户端运行实例标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the client instance identifier used later to validate resume attempts from the same runtime instance.
    /// </remarks>
    /// <value>客户端运行实例标识 / Client runtime instance identifier</value>
    public ClientInstanceId ClientInstanceId { get; init; }

    /// <summary>
    /// 获取或设置客户端请求的 ReliableSession 协议版本。
    /// </summary>
    /// <remarks>
    /// Gets or sets the protocol version requested by the client.
    /// </remarks>
    /// <value>请求的协议版本 / Requested protocol version</value>
    public ushort ProtocolVersion { get; init; }

    /// <summary>
    /// 获取或设置客户端请求协商的可靠会话配置。
    /// </summary>
    /// <remarks>
    /// Gets or sets the requested options for heartbeat, recovery, replay, deduplication, and snapshot fallback.
    /// </remarks>
    /// <value>请求协商的配置 / Requested handshake options</value>
    public ReliableSessionHandshakeOptions RequestedOptions { get; init; } = new();
}

/// <summary>
/// 表示服务端接受新逻辑会话后的握手确认帧。
/// </summary>
/// <remarks>
/// Represents the server hello acknowledgement that returns the logical session id, first physical connection id, initial resume generation, resume token, and negotiated options.
/// </remarks>
public sealed record ReliableSessionHelloAckFrame : ReliableSessionFrame
{
    /// <summary>
    /// 获取协议帧类型。
    /// </summary>
    /// <remarks>
    /// Gets the frame kind for a hello acknowledgement.
    /// </remarks>
    /// <value>固定为 <see cref="ReliableSessionFrameKind.HelloAck"/> / Always <see cref="ReliableSessionFrameKind.HelloAck"/></value>
    public override ReliableSessionFrameKind Kind => ReliableSessionFrameKind.HelloAck;

    /// <summary>
    /// 获取或设置恢复窗口内保持稳定的逻辑会话标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the logical session identifier that must remain stable across resumed physical connections.
    /// </remarks>
    /// <value>逻辑会话标识 / Logical session identifier</value>
    public SessionId SessionId { get; init; }

    /// <summary>
    /// 获取或设置首次物理连接标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the identifier for the accepted physical connection that created this logical session.
    /// </remarks>
    /// <value>物理连接标识 / Physical connection identifier</value>
    public ConnectionId ConnectionId { get; init; }

    /// <summary>
    /// 获取或设置初始恢复代次。
    /// </summary>
    /// <remarks>
    /// Gets or sets the initial resume generation used to reject stale frames after future resumes.
    /// </remarks>
    /// <value>初始恢复代次 / Initial resume generation</value>
    public ResumeGeneration ResumeGeneration { get; init; }

    /// <summary>
    /// 获取或设置服务端签发的不透明恢复令牌。
    /// </summary>
    /// <remarks>
    /// Gets or sets the opaque token required to authorize later resume attempts.
    /// </remarks>
    /// <value>恢复令牌 / Resume token</value>
    public ResumeToken ResumeToken { get; init; }

    /// <summary>
    /// 获取或设置服务端确认后的可靠会话配置。
    /// </summary>
    /// <remarks>
    /// Gets or sets the negotiated heartbeat, replay, deduplication, token, and snapshot options.
    /// </remarks>
    /// <value>协商后的配置 / Negotiated options</value>
    public ReliableSessionHandshakeOptions NegotiatedOptions { get; init; } = new();
}

/// <summary>
/// 表示在新物理连接上恢复已有逻辑会话的请求帧。
/// </summary>
/// <remarks>
/// Represents a resume request carrying the stable logical session id, resume token, new physical connection id, receive cursor, committed cursor, and optional snapshot baseline.
/// </remarks>
public sealed record ReliableSessionResumeFrame : ReliableSessionFrame
{
    /// <summary>
    /// 获取协议帧类型。
    /// </summary>
    /// <remarks>
    /// Gets the frame kind for a resume request.
    /// </remarks>
    /// <value>固定为 <see cref="ReliableSessionFrameKind.Resume"/> / Always <see cref="ReliableSessionFrameKind.Resume"/></value>
    public override ReliableSessionFrameKind Kind => ReliableSessionFrameKind.Resume;

    /// <summary>
    /// 获取或设置要恢复的逻辑会话标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the logical session identifier that survives the physical reconnect.
    /// </remarks>
    /// <value>逻辑会话标识 / Logical session identifier</value>
    public SessionId SessionId { get; init; }

    /// <summary>
    /// 获取或设置发起恢复的客户端运行实例标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the client instance identifier used to validate that the resume request comes from the expected runtime instance.
    /// </remarks>
    /// <value>客户端运行实例标识 / Client runtime instance identifier</value>
    public ClientInstanceId ClientInstanceId { get; init; }

    /// <summary>
    /// 获取或设置本次恢复使用的新物理连接标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the new physical connection identifier; it must differ from the previous closed connection.
    /// </remarks>
    /// <value>新物理连接标识 / New physical connection identifier</value>
    public ConnectionId ConnectionId { get; init; }

    /// <summary>
    /// 获取或设置用于恢复校验的不透明恢复令牌。
    /// </summary>
    /// <remarks>
    /// Gets or sets the resume token that authorizes endpoint migration and physical connection replacement.
    /// </remarks>
    /// <value>恢复令牌 / Resume token</value>
    public ResumeToken ResumeToken { get; init; }

    /// <summary>
    /// 获取或设置接收方已经连续接收的游标。
    /// </summary>
    /// <remarks>
    /// Gets or sets the receive cursor used by the peer to decide the replay start.
    /// </remarks>
    /// <value>接收游标 / Receive cursor</value>
    public Sequence ReceiveCursor { get; init; }

    /// <summary>
    /// 获取或设置业务层已经提交处理的游标。
    /// </summary>
    /// <remarks>
    /// Gets or sets the committed cursor that can be used by business code to avoid repeating side effects.
    /// </remarks>
    /// <value>已提交游标 / Committed cursor</value>
    public Sequence CommittedCursor { get; init; }

    /// <summary>
    /// 获取或设置是否携带快照版本。
    /// </summary>
    /// <remarks>
    /// Gets or sets whether the resume request carries a snapshot baseline.
    /// </remarks>
    /// <value>如果携带快照版本则为 <c>true</c>；否则为 <c>false</c> / <c>true</c> if a snapshot version is present; otherwise <c>false</c></value>
    public bool HasSnapshotVersion { get; init; }

    /// <summary>
    /// 获取或设置恢复请求携带的快照版本。
    /// </summary>
    /// <remarks>
    /// Gets or sets the snapshot baseline known by the receiver when replay alone may be insufficient.
    /// </remarks>
    /// <value>快照版本 / Snapshot version</value>
    public SnapshotVersion SnapshotVersion { get; init; } = new SnapshotVersion(-1);
}

/// <summary>
/// 表示服务端确认逻辑会话恢复后的响应帧。
/// </summary>
/// <remarks>
/// Represents a resume acknowledgement that advances the resume generation, declares the replay start, indicates snapshot fallback, and optionally rotates the resume token.
/// </remarks>
public sealed record ReliableSessionResumeAckFrame : ReliableSessionFrame
{
    /// <summary>
    /// 获取协议帧类型。
    /// </summary>
    /// <remarks>
    /// Gets the frame kind for a resume acknowledgement.
    /// </remarks>
    /// <value>固定为 <see cref="ReliableSessionFrameKind.ResumeAck"/> / Always <see cref="ReliableSessionFrameKind.ResumeAck"/></value>
    public override ReliableSessionFrameKind Kind => ReliableSessionFrameKind.ResumeAck;

    /// <summary>
    /// 获取或设置已恢复的逻辑会话标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the logical session identifier accepted by the resume flow.
    /// </remarks>
    /// <value>逻辑会话标识 / Logical session identifier</value>
    public SessionId SessionId { get; init; }

    /// <summary>
    /// 获取或设置恢复成功后的物理连接标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the physical connection identifier now bound to the resumed logical session.
    /// </remarks>
    /// <value>物理连接标识 / Physical connection identifier</value>
    public ConnectionId ConnectionId { get; init; }

    /// <summary>
    /// 获取或设置恢复成功后的新代次。
    /// </summary>
    /// <remarks>
    /// Gets or sets the new resume generation; frames from older generations should be ignored by runtime implementations.
    /// </remarks>
    /// <value>新恢复代次 / New resume generation</value>
    public ResumeGeneration ResumeGeneration { get; init; }

    /// <summary>
    /// 获取或设置恢复后需要开始重放的序号。
    /// </summary>
    /// <remarks>
    /// Gets or sets the first sequence that should be replayed after the resume is accepted.
    /// </remarks>
    /// <value>重放起始序号 / Replay start sequence</value>
    public Sequence ReplayStart { get; init; }

    /// <summary>
    /// 获取或设置恢复前是否必须先应用快照。
    /// </summary>
    /// <remarks>
    /// Gets or sets whether snapshot fallback is required because replay cannot safely cover the receiver gap.
    /// </remarks>
    /// <value>如果必须先应用快照则为 <c>true</c>；否则为 <c>false</c> / <c>true</c> if snapshot fallback is required; otherwise <c>false</c></value>
    public bool SnapshotRequired { get; init; }

    /// <summary>
    /// 获取或设置恢复令牌是否已轮换。
    /// </summary>
    /// <remarks>
    /// Gets or sets whether the resume token returned by this frame replaces the previous token.
    /// </remarks>
    /// <value>如果已轮换令牌则为 <c>true</c>；否则为 <c>false</c> / <c>true</c> if the token was rotated; otherwise <c>false</c></value>
    public bool RotateResumeToken { get; init; }

    /// <summary>
    /// 获取或设置当前有效的不透明恢复令牌。
    /// </summary>
    /// <remarks>
    /// Gets or sets the currently valid resume token after the resume acknowledgement.
    /// </remarks>
    /// <value>当前恢复令牌 / Current resume token</value>
    public ResumeToken ResumeToken { get; init; }

    /// <summary>
    /// 获取或设置恢复后继续使用的协商配置。
    /// </summary>
    /// <remarks>
    /// Gets or sets the negotiated options that remain active after resume.
    /// </remarks>
    /// <value>协商后的配置 / Negotiated options</value>
    public ReliableSessionHandshakeOptions NegotiatedOptions { get; init; } = new();
}

/// <summary>
/// 表示 ReliableSession 逻辑会话心跳帧。
/// </summary>
/// <remarks>
/// Represents a logical-session heartbeat frame; it observes liveness above the transport and is not part of KCP Core.
/// </remarks>
public sealed record ReliableSessionHeartbeatFrame : ReliableSessionFrame
{
    /// <summary>
    /// 获取协议帧类型。
    /// </summary>
    /// <remarks>
    /// Gets the frame kind for a heartbeat.
    /// </remarks>
    /// <value>固定为 <see cref="ReliableSessionFrameKind.Heartbeat"/> / Always <see cref="ReliableSessionFrameKind.Heartbeat"/></value>
    public override ReliableSessionFrameKind Kind => ReliableSessionFrameKind.Heartbeat;

    /// <summary>
    /// 获取或设置心跳所属的逻辑会话标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the logical session identifier observed by this heartbeat.
    /// </remarks>
    /// <value>逻辑会话标识 / Logical session identifier</value>
    public SessionId SessionId { get; init; }

    /// <summary>
    /// 获取或设置心跳经过的物理连接标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the physical connection identifier used to detect stale heartbeat frames.
    /// </remarks>
    /// <value>物理连接标识 / Physical connection identifier</value>
    public ConnectionId ConnectionId { get; init; }

    /// <summary>
    /// 获取或设置发送方已发送的最新序号。
    /// </summary>
    /// <remarks>
    /// Gets or sets the latest sequence sent by this side.
    /// </remarks>
    /// <value>已发送的最新序号 / Latest sent sequence</value>
    public Sequence LastSentSequence { get; init; }

    /// <summary>
    /// 获取或设置发送方已收到确认的最新序号。
    /// </summary>
    /// <remarks>
    /// Gets or sets the latest sequence acknowledged by the peer.
    /// </remarks>
    /// <value>已确认的最新序号 / Latest acknowledged sequence</value>
    public Sequence LastAckedSequence { get; init; }
}

/// <summary>
/// 表示承载业务负载的 ReliableSession 数据帧。
/// </summary>
/// <remarks>
/// Represents an ordered business data frame carrying the message id needed for deduplication, the sequence needed for ordered delivery, and the payload passed to later runtime delivery.
/// </remarks>
public sealed record ReliableSessionDataFrame : ReliableSessionFrame
{
    /// <summary>
    /// 获取协议帧类型。
    /// </summary>
    /// <remarks>
    /// Gets the frame kind for business data.
    /// </remarks>
    /// <value>固定为 <see cref="ReliableSessionFrameKind.Data"/> / Always <see cref="ReliableSessionFrameKind.Data"/></value>
    public override ReliableSessionFrameKind Kind => ReliableSessionFrameKind.Data;

    /// <summary>
    /// 获取或设置业务数据所属的逻辑会话标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the logical session identifier that scopes message id deduplication and sequence ordering.
    /// </remarks>
    /// <value>逻辑会话标识 / Logical session identifier</value>
    public SessionId SessionId { get; init; }

    /// <summary>
    /// 获取或设置业务消息标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the message identifier used by deduplication so repeated frames can be acknowledged without duplicate business delivery.
    /// </remarks>
    /// <value>业务消息标识 / Business message identifier</value>
    public MessageId MessageId { get; init; }

    /// <summary>
    /// 获取或设置有序投递序号。
    /// </summary>
    /// <remarks>
    /// Gets or sets the sequence number used for ordered delivery, gap detection, replay, and acknowledgement.
    /// </remarks>
    /// <value>有序投递序号 / Ordered delivery sequence</value>
    public Sequence Sequence { get; init; }

    /// <summary>
    /// 获取或设置是否携带快照版本。
    /// </summary>
    /// <remarks>
    /// Gets or sets whether the data frame is tied to a snapshot baseline.
    /// </remarks>
    /// <value>如果携带快照版本则为 <c>true</c>；否则为 <c>false</c> / <c>true</c> if a snapshot version is present; otherwise <c>false</c></value>
    public bool HasSnapshotVersion { get; init; }

    /// <summary>
    /// 获取或设置业务数据对应的快照版本。
    /// </summary>
    /// <remarks>
    /// Gets or sets the snapshot version that anchors this data frame after snapshot fallback.
    /// </remarks>
    /// <value>快照版本 / Snapshot version</value>
    public SnapshotVersion SnapshotVersion { get; init; } = new SnapshotVersion(-1);

    /// <summary>
    /// 获取或设置业务负载字节。
    /// </summary>
    /// <remarks>
    /// Gets or sets the business payload; C3 only models and encodes it and does not deliver it to package handlers.
    /// </remarks>
    /// <value>业务负载字节 / Business payload bytes</value>
    public byte[] Payload { get; init; } = Array.Empty<byte>();
}

/// <summary>
/// 表示确认一个或多个连续序号范围的确认帧。
/// </summary>
/// <remarks>
/// Represents acknowledgement ranges that allow the sender to release replay-window entries after data is accepted.
/// </remarks>
public sealed record ReliableSessionAckFrame : ReliableSessionFrame
{
    /// <summary>
    /// 获取协议帧类型。
    /// </summary>
    /// <remarks>
    /// Gets the frame kind for acknowledgements.
    /// </remarks>
    /// <value>固定为 <see cref="ReliableSessionFrameKind.Ack"/> / Always <see cref="ReliableSessionFrameKind.Ack"/></value>
    public override ReliableSessionFrameKind Kind => ReliableSessionFrameKind.Ack;

    /// <summary>
    /// 获取或设置确认帧所属的逻辑会话标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the logical session identifier that scopes the acknowledged sequences.
    /// </remarks>
    /// <value>逻辑会话标识 / Logical session identifier</value>
    public SessionId SessionId { get; init; }

    /// <summary>
    /// 获取或设置已确认的连续序号范围。
    /// </summary>
    /// <remarks>
    /// Gets or sets sorted, non-overlapping acknowledgement ranges used to clean the replay window.
    /// </remarks>
    /// <value>确认范围集合 / Acknowledgement ranges</value>
    public AckRange[] Ranges { get; init; } = Array.Empty<AckRange>();
}

/// <summary>
/// 表示请求业务快照兜底恢复的帧。
/// </summary>
/// <remarks>
/// Represents a request for snapshot fallback when replay is insufficient or the receiver needs a known state baseline before applying later data.
/// </remarks>
public sealed record ReliableSessionSnapshotRequestFrame : ReliableSessionFrame
{
    /// <summary>
    /// 获取协议帧类型。
    /// </summary>
    /// <remarks>
    /// Gets the frame kind for a snapshot request.
    /// </remarks>
    /// <value>固定为 <see cref="ReliableSessionFrameKind.SnapshotRequest"/> / Always <see cref="ReliableSessionFrameKind.SnapshotRequest"/></value>
    public override ReliableSessionFrameKind Kind => ReliableSessionFrameKind.SnapshotRequest;

    /// <summary>
    /// 获取或设置请求快照的逻辑会话标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the logical session identifier that needs snapshot recovery.
    /// </remarks>
    /// <value>逻辑会话标识 / Logical session identifier</value>
    public SessionId SessionId { get; init; }

    /// <summary>
    /// 获取或设置希望从哪个序号继续重放。
    /// </summary>
    /// <remarks>
    /// Gets or sets the sequence from which replay should continue after the snapshot baseline is applied.
    /// </remarks>
    /// <value>重放起始序号 / Replay start sequence</value>
    public Sequence FromSequence { get; init; }

    /// <summary>
    /// 获取或设置接收方当前持有的快照版本。
    /// </summary>
    /// <remarks>
    /// Gets or sets the snapshot version currently known by the receiver.
    /// </remarks>
    /// <value>快照版本 / Snapshot version</value>
    public SnapshotVersion SnapshotVersion { get; init; }

    /// <summary>
    /// 获取或设置是否因为重放窗口不足而请求快照。
    /// </summary>
    /// <remarks>
    /// Gets or sets whether replay-window insufficiency caused the snapshot request.
    /// </remarks>
    /// <value>如果重放窗口不足则为 <c>true</c>；否则为 <c>false</c> / <c>true</c> if the replay window was insufficient; otherwise <c>false</c></value>
    public bool ReplayWindowInsufficient { get; init; }
}

/// <summary>
/// 表示承载业务快照内容的帧。
/// </summary>
/// <remarks>
/// Represents a snapshot payload that establishes a recovery baseline before incremental replay continues.
/// </remarks>
public sealed record ReliableSessionSnapshotFrame : ReliableSessionFrame
{
    /// <summary>
    /// 获取协议帧类型。
    /// </summary>
    /// <remarks>
    /// Gets the frame kind for a snapshot payload.
    /// </remarks>
    /// <value>固定为 <see cref="ReliableSessionFrameKind.Snapshot"/> / Always <see cref="ReliableSessionFrameKind.Snapshot"/></value>
    public override ReliableSessionFrameKind Kind => ReliableSessionFrameKind.Snapshot;

    /// <summary>
    /// 获取或设置快照所属的逻辑会话标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the logical session identifier that owns this snapshot baseline.
    /// </remarks>
    /// <value>逻辑会话标识 / Logical session identifier</value>
    public SessionId SessionId { get; init; }

    /// <summary>
    /// 获取或设置快照版本。
    /// </summary>
    /// <remarks>
    /// Gets or sets the snapshot version delivered by this frame.
    /// </remarks>
    /// <value>快照版本 / Snapshot version</value>
    public SnapshotVersion SnapshotVersion { get; init; }

    /// <summary>
    /// 获取或设置该快照覆盖到的基础序号。
    /// </summary>
    /// <remarks>
    /// Gets or sets the base sequence covered by the snapshot; replay should continue after this boundary.
    /// </remarks>
    /// <value>快照覆盖的基础序号 / Base sequence covered by the snapshot</value>
    public Sequence BaseSequence { get; init; }

    /// <summary>
    /// 获取或设置业务快照负载字节。
    /// </summary>
    /// <remarks>
    /// Gets or sets the snapshot payload; C3 only encodes the bytes and does not interpret business state.
    /// </remarks>
    /// <value>业务快照负载字节 / Snapshot payload bytes</value>
    public byte[] Payload { get; init; } = Array.Empty<byte>();
}

/// <summary>
/// 表示关闭逻辑会话的协议帧。
/// </summary>
/// <remarks>
/// Represents a logical-session close frame; it is distinct from closing one physical TCP, KCP, UDP, or WebSocket connection.
/// </remarks>
public sealed record ReliableSessionCloseFrame : ReliableSessionFrame
{
    /// <summary>
    /// 获取协议帧类型。
    /// </summary>
    /// <remarks>
    /// Gets the frame kind for logical session close.
    /// </remarks>
    /// <value>固定为 <see cref="ReliableSessionFrameKind.Close"/> / Always <see cref="ReliableSessionFrameKind.Close"/></value>
    public override ReliableSessionFrameKind Kind => ReliableSessionFrameKind.Close;

    /// <summary>
    /// 获取或设置要关闭的逻辑会话标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the logical session identifier being closed.
    /// </remarks>
    /// <value>逻辑会话标识 / Logical session identifier</value>
    public SessionId SessionId { get; init; }

    /// <summary>
    /// 获取或设置逻辑会话关闭原因。
    /// </summary>
    /// <remarks>
    /// Gets or sets why the logical session is closing.
    /// </remarks>
    /// <value>关闭原因 / Close reason</value>
    public ReliableSessionCloseReason CloseReason { get; init; }

    /// <summary>
    /// 获取或设置是否携带协议错误码。
    /// </summary>
    /// <remarks>
    /// Gets or sets whether the close frame includes a protocol error code.
    /// </remarks>
    /// <value>如果携带错误码则为 <c>true</c>；否则为 <c>false</c> / <c>true</c> if an error code is present; otherwise <c>false</c></value>
    public bool HasErrorCode { get; init; }

    /// <summary>
    /// 获取或设置逻辑会话关闭关联的协议错误码。
    /// </summary>
    /// <remarks>
    /// Gets or sets the protocol error code associated with the close frame when present.
    /// </remarks>
    /// <value>协议错误码 / Protocol error code</value>
    public ReliableSessionErrorCode ErrorCode { get; init; }

    /// <summary>
    /// 获取或设置关闭说明。
    /// </summary>
    /// <remarks>
    /// Gets or sets a diagnostic close message.
    /// </remarks>
    /// <value>关闭说明 / Close message</value>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// 表示 ReliableSession 协议错误帧。
/// </summary>
/// <remarks>
/// Represents a protocol-level error frame; it must not be delivered as a business package to upper handlers.
/// </remarks>
public sealed record ReliableSessionErrorFrame : ReliableSessionFrame
{
    /// <summary>
    /// 获取协议帧类型。
    /// </summary>
    /// <remarks>
    /// Gets the frame kind for protocol errors.
    /// </remarks>
    /// <value>固定为 <see cref="ReliableSessionFrameKind.Error"/> / Always <see cref="ReliableSessionFrameKind.Error"/></value>
    public override ReliableSessionFrameKind Kind => ReliableSessionFrameKind.Error;

    /// <summary>
    /// 获取或设置发生协议错误的逻辑会话标识。
    /// </summary>
    /// <remarks>
    /// Gets or sets the logical session identifier associated with the protocol error.
    /// </remarks>
    /// <value>逻辑会话标识 / Logical session identifier</value>
    public SessionId SessionId { get; init; }

    /// <summary>
    /// 获取或设置协议错误码。
    /// </summary>
    /// <remarks>
    /// Gets or sets the protocol error code.
    /// </remarks>
    /// <value>协议错误码 / Protocol error code</value>
    public ReliableSessionErrorCode ErrorCode { get; init; }

    /// <summary>
    /// 获取或设置是否携带关闭原因。
    /// </summary>
    /// <remarks>
    /// Gets or sets whether the error frame includes the logical-session close reason.
    /// </remarks>
    /// <value>如果携带关闭原因则为 <c>true</c>；否则为 <c>false</c> / <c>true</c> if a close reason is present; otherwise <c>false</c></value>
    public bool HasCloseReason { get; init; }

    /// <summary>
    /// 获取或设置协议错误关联的关闭原因。
    /// </summary>
    /// <remarks>
    /// Gets or sets the close reason associated with this protocol error when present.
    /// </remarks>
    /// <value>关闭原因 / Close reason</value>
    public ReliableSessionCloseReason CloseReason { get; init; }

    /// <summary>
    /// 获取或设置协议错误说明。
    /// </summary>
    /// <remarks>
    /// Gets or sets a diagnostic protocol error message.
    /// </remarks>
    /// <value>错误说明 / Error message</value>
    public string Message { get; init; } = string.Empty;
}
