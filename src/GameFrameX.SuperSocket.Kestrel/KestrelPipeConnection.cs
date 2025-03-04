﻿using System.IO;
using System.Net.Sockets;
using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.ProtoBase;

namespace GameFrameX.SuperSocket.Kestrel;

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

public class KestrelPipeConnection : PipeConnectionBase
{
    private ConnectionContext _context;

    public KestrelPipeConnection(ConnectionContext context, ConnectionOptions options)
        : base(context.Transport.Input, context.Transport.Output, options)
    {
        _context = context;
        context.ConnectionClosed.Register(() => OnConnectionClosed());
        LocalEndPoint = context.LocalEndPoint;
        RemoteEndPoint = context.RemoteEndPoint;
    }

    protected override async ValueTask CompleteReaderAsync(PipeReader reader, bool isDetaching)
    {
        if (!isDetaching)
        {
            await reader.CompleteAsync().ConfigureAwait(false);
        }
    }

    protected override async ValueTask CompleteWriterAsync(PipeWriter writer, bool isDetaching)
    {
        if (!isDetaching)
        {
            await writer.CompleteAsync().ConfigureAwait(false);
        }
    }

    protected override void OnClosed()
    {
        if (!CloseReason.HasValue)
            CloseReason = Connection.CloseReason.RemoteClosing;

        base.OnClosed();
    }

    protected override async void Close()
    {
        var context = _context;

        if (context == null)
            return;

        if (Interlocked.CompareExchange(ref _context, null, context) == context)
        {
            await context.DisposeAsync();
        }
    }

    protected override void OnInputPipeRead(ReadResult result)
    {
        if (!result.IsCanceled && !result.IsCompleted)
        {
            UpdateLastActiveTime();
        }
    }

    public override async ValueTask SendAsync(Action<PipeWriter> write, CancellationToken cancellationToken = default)
    {
        await base.SendAsync(write, cancellationToken);
        UpdateLastActiveTime();
    }

    public override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await base.SendAsync(buffer, cancellationToken);
        UpdateLastActiveTime();
    }

    public override async ValueTask SendAsync<TPackage>(IPackageEncoder<TPackage> packageEncoder, TPackage package, CancellationToken cancellationToken = default)
    {
        await base.SendAsync(packageEncoder, package, cancellationToken);
        UpdateLastActiveTime();
    }

    protected override bool IsIgnorableException(Exception e)
    {
        if (e is IOException { InnerException: not null } ioException)
        {
            return IsIgnorableException(ioException.InnerException);
        }

        if (e is SocketException se)
        {
            return se.IsIgnorableSocketException();
        }

        return base.IsIgnorableException(e);
    }

    private void OnConnectionClosed()
    {
        CancelAsync().Wait();
    }
}