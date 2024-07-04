using System;
using System.Net.Sockets;
using GameFrameX.SuperSocket.Connection;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;

namespace GameFrameX.SuperSocket.Kestrel;

public class KestrelPipeConnectionFactoryBuilder : IConnectionFactoryBuilder
{
    private readonly SocketConnectionContextFactory _socketConnectionContextFactory;

    private readonly Action<Socket> _socketOptionsSetter;

    public KestrelPipeConnectionFactoryBuilder(SocketConnectionContextFactory socketConnectionContextFactory, SocketOptionsSetter socketOptionsSetter)
    {
        _socketConnectionContextFactory = socketConnectionContextFactory;
        _socketOptionsSetter = socketOptionsSetter.Setter;
    }

    public IConnectionFactory Build(ListenOptions listenOptions, ConnectionOptions connectionOptions)
    {
        return new KestrelPipeConnectionFactory(_socketConnectionContextFactory, listenOptions, connectionOptions, _socketOptionsSetter);
    }
}