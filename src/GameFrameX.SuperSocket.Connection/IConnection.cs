﻿using System.IO.Pipelines;
using System.Net;
using GameFrameX.SuperSocket.ProtoBase;
using GameFrameX.SuperSocket.ProtoBase.ProxyProtocol;

namespace GameFrameX.SuperSocket.Connection
{
    public interface IConnection
    {
        IAsyncEnumerable<TPackageInfo> RunAsync<TPackageInfo>(IPipelineFilter<TPackageInfo> pipelineFilter);

        ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

        ValueTask SendAsync<TPackage>(IPackageEncoder<TPackage> packageEncoder, TPackage package, CancellationToken cancellationToken = default);

        ValueTask SendAsync(Action<PipeWriter> write, CancellationToken cancellationToken = default);

        ValueTask CloseAsync(CloseReason closeReason);

        event EventHandler<CloseEventArgs> Closed;

        bool IsClosed { get; }

        EndPoint RemoteEndPoint { get; }

        EndPoint LocalEndPoint { get; }

        DateTimeOffset LastActiveTime { get; }

        ValueTask DetachAsync();

        CloseReason? CloseReason { get; }
        CancellationToken ConnectionToken { get; }
        ProxyInfo ProxyInfo { get; }
    }
}