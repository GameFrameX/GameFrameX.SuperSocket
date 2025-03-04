using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GameFrameX.SuperSocket.ProtoBase;
using System.IO.Compression;
using GameFrameX.SuperSocket.Connection;

namespace GameFrameX.SuperSocket.Client;

/// <summary>
/// 支持发送和接收不同包类型的简单客户端实现
/// </summary>
/// <typeparam name="TPackage">接收包类型</typeparam>
/// <typeparam name="TSendPackage">发送包类型</typeparam>
public class EasyClient<TPackage, TSendPackage> : EasyClient<TPackage>, IEasyClient<TPackage, TSendPackage>
    where TPackage : class
{
    private IPackageEncoder<TSendPackage> _packageEncoder;

    /// <summary>
    /// 使用指定的包编码器初始化客户端
    /// </summary>
    /// <param name="packageEncoder">包编码器</param>
    protected EasyClient(IPackageEncoder<TSendPackage> packageEncoder)
        : base()
    {
        _packageEncoder = packageEncoder;
    }

    /// <summary>
    /// 使用指定的管道过滤器、包编码器和日志记录器初始化客户端
    /// </summary>
    /// <param name="pipelineFilter">管道过滤器</param>
    /// <param name="packageEncoder">包编码器</param>
    /// <param name="logger">日志记录器</param>
    public EasyClient(IPipelineFilter<TPackage> pipelineFilter, IPackageEncoder<TSendPackage> packageEncoder, ILogger logger = null)
        : this(pipelineFilter, packageEncoder, new ConnectionOptions { Logger = logger })
    {
    }

    /// <summary>
    /// 使用指定的管道过滤器、包编码器和连接选项初始化客户端
    /// </summary>
    /// <param name="pipelineFilter">管道过滤器</param>
    /// <param name="packageEncoder">包编码器</param>
    /// <param name="options">连接选项</param>
    public EasyClient(IPipelineFilter<TPackage> pipelineFilter, IPackageEncoder<TSendPackage> packageEncoder, ConnectionOptions options)
        : base(pipelineFilter, options)
    {
        _packageEncoder = packageEncoder;
    }

    /// <summary>
    /// 异步发送数据包
    /// </summary>
    /// <param name="package">要发送的数据包</param>
    public virtual async ValueTask SendAsync(TSendPackage package)
    {
        await SendAsync(_packageEncoder, package);
    }

    /// <summary>
    /// 将当前实例转换为客户端接口
    /// </summary>
    /// <returns>客户端接口实例</returns>
    public new IEasyClient<TPackage, TSendPackage> AsClient()
    {
        return this;
    }
}

/// <summary>
/// 简单客户端的基类实现
/// </summary>
/// <typeparam name="TReceivePackage">接收包类型</typeparam>
public class EasyClient<TReceivePackage> : IEasyClient<TReceivePackage>
    where TReceivePackage : class
{
    private IPipelineFilter<TReceivePackage> _pipelineFilter;

    /// <summary>
    /// 获取或设置连接实例
    /// </summary>
    protected IConnection Connection { get; private set; }

    /// <summary>
    /// 获取或设置日志记录器
    /// </summary>
    protected ILogger Logger { get; set; }

    /// <summary>
    /// 获取或设置连接选项
    /// </summary>
    protected ConnectionOptions Options { get; private set; }

    IAsyncEnumerator<TReceivePackage> _packageStream;

    /// <summary>
    /// 数据包处理事件
    /// </summary>
    public event PackageHandler<TReceivePackage> PackageHandler;

    /// <summary>
    /// 获取或设置本地终结点
    /// </summary>
    public IPEndPoint LocalEndPoint { get; set; }

    /// <summary>
    /// 获取或设置安全选项
    /// </summary>
    public SecurityOptions Security { get; set; }

    /// <summary>
    /// 获取或设置压缩级别
    /// </summary>
    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.NoCompression;

    /// <summary>
    /// 保护的构造函数
    /// </summary>
    protected EasyClient()
    {
    }

    /// <summary>
    /// 使用指定的管道过滤器初始化客户端
    /// </summary>
    /// <param name="pipelineFilter">管道过滤器</param>
    public EasyClient(IPipelineFilter<TReceivePackage> pipelineFilter)
        : this(pipelineFilter, NullLogger.Instance)
    {
    }

    /// <summary>
    /// 使用指定的管道过滤器和日志记录器初始化客户端
    /// </summary>
    /// <param name="pipelineFilter">管道过滤器</param>
    /// <param name="logger">日志记录器</param>
    public EasyClient(IPipelineFilter<TReceivePackage> pipelineFilter, ILogger logger)
        : this(pipelineFilter, new ConnectionOptions { Logger = logger })
    {
    }

    /// <summary>
    /// 使用指定的管道过滤器和连接选项初始化客户端
    /// </summary>
    /// <param name="pipelineFilter">管道过滤器</param>
    /// <param name="options">连接选项</param>
    public EasyClient(IPipelineFilter<TReceivePackage> pipelineFilter, ConnectionOptions options)
    {
        if (pipelineFilter == null)
            throw new ArgumentNullException(nameof(pipelineFilter));
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        _pipelineFilter = pipelineFilter;
        Options = options;
        Logger = options.Logger;
    }

    /// <summary>
    /// 将当前实例转换为客户端接口
    /// </summary>
    /// <returns>客户端接口实例</returns>
    public virtual IEasyClient<TReceivePackage> AsClient()
    {
        return this;
    }

    /// <summary>
    /// 获取连接器
    /// </summary>
    /// <returns>连接器实例</returns>
    protected virtual IConnector GetConnector()
    {
        var connectors = new List<IConnector>();
        connectors.Add(new SocketConnector(LocalEndPoint));
        var security = Security;
        if (security != null)
        {
            if (security.EnabledSslProtocols != SslProtocols.None)
                connectors.Add(new SslStreamConnector(security));
        }

        if (CompressionLevel != CompressionLevel.NoCompression)
        {
            connectors.Add(new GZipConnector(CompressionLevel));
        }

        return BuildConnectors(connectors);
    }

    /// <summary>
    /// 构建连接器链
    /// </summary>
    /// <param name="connectors">连接器集合</param>
    /// <returns>第一个连接器</returns>
    protected IConnector BuildConnectors(IEnumerable<IConnector> connectors)
    {
        var prevConnector = default(ConnectorBase);
        foreach (var connector in connectors)
        {
            if (prevConnector != null)
            {
                prevConnector.NextConnector = connector;
            }

            prevConnector = connector as ConnectorBase;
        }

        return connectors.First();
    }

    ValueTask<bool> IEasyClient<TReceivePackage>.ConnectAsync(EndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        return ConnectAsync(remoteEndPoint, cancellationToken);
    }

    /// <summary>
    /// 异步连接到远程终结点
    /// </summary>
    /// <param name="remoteEndPoint">远程终结点</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接是否成功</returns>
    protected virtual async ValueTask<bool> ConnectAsync(EndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        var connector = GetConnector();
        var state = await connector.ConnectAsync(remoteEndPoint, null, cancellationToken);
        if (state.Cancelled || cancellationToken.IsCancellationRequested)
        {
            OnError($"The connection to {remoteEndPoint} was cancelled.", state.Exception);
            return false;
        }

        if (!state.Result)
        {
            OnError($"Failed to connect to {remoteEndPoint}", state.Exception);
            return false;
        }

        var socket = state.Socket;
        if (socket == null)
            throw new Exception("Socket is null.");
        SetupConnection(state.CreateConnection(Options));
        return true;
    }

    /// <summary>
    /// 配置为UDP模式
    /// </summary>
    /// <param name="remoteEndPoint">远程终结点</param>
    /// <param name="bufferPool">缓冲池</param>
    /// <param name="bufferSize">缓冲区大小</param>
    public void AsUdp(IPEndPoint remoteEndPoint, ArrayPool<byte> bufferPool = null, int bufferSize = 4096)
    {
        var localEndPoint = LocalEndPoint;
        if (localEndPoint == null)
        {
            localEndPoint = new IPEndPoint(remoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
        }

        var socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        // bind the local endpoint
        socket.Bind(localEndPoint);
        var connection = new UdpPipeConnection(socket, this.Options, remoteEndPoint);
        SetupConnection(connection);
        UdpReceive(socket, connection, bufferPool, bufferSize);
    }

    /// <summary>
    /// UDP数据接收处理
    /// </summary>
    private async void UdpReceive(Socket socket, UdpPipeConnection connection, ArrayPool<byte> bufferPool, int bufferSize)
    {
        if (bufferPool == null)
            bufferPool = ArrayPool<byte>.Shared;
        while (true)
        {
            var buffer = bufferPool.Rent(bufferSize);
            try
            {
                var result = await socket
                                   .ReceiveFromAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), SocketFlags.None, connection.RemoteEndPoint)
                                   .ConfigureAwait(false);
                await connection.WritePipeDataAsync((new ArraySegment<byte>(buffer, 0, result.ReceivedBytes)).AsMemory(), CancellationToken.None);
            }
            catch (NullReferenceException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception e)
            {
                OnError($"Failed to receive UDP data.", e);
            }
            finally
            {
                bufferPool.Return(buffer);
            }
        }
    }

    /// <summary>
    /// 设置连接
    /// </summary>
    /// <param name="connection">连接实例</param>
    protected virtual void SetupConnection(IConnection connection)
    {
        connection.Closed += OnConnectionClosed;
        _packageStream = connection.GetPackageStream(_pipelineFilter);
        Connection = connection;
    }

    /// <summary>
    /// 接收数据包的接口实现
    /// </summary>
    /// <returns>接收到的数据包</returns>
    ValueTask<TReceivePackage> IEasyClient<TReceivePackage>.ReceiveAsync()
    {
        return ReceiveAsync();
    }

    /// <summary>
    /// 尝试接收一个数据包
    /// </summary>
    /// <returns>接收到的数据包，如果连接关闭则返回null</returns>
    protected virtual async ValueTask<TReceivePackage> ReceiveAsync()
    {
        var p = await _packageStream.ReceiveAsync();

        if (p != null)
            return p;

        OnClosed(Connection, EventArgs.Empty);
        return null;
    }

    /// <summary>
    /// 开始接收数据包的接口实现
    /// </summary>
    void IEasyClient<TReceivePackage>.StartReceive()
    {
        StartReceive();
    }

    /// <summary>
    /// 开始接收数据包并通过事件处理器处理
    /// </summary>
    protected virtual void StartReceive()
    {
        StartReceiveAsync();
    }

    /// <summary>
    /// 异步开始接收数据包
    /// </summary>
    private async void StartReceiveAsync()
    {
        var enumerator = _packageStream;

        while (await enumerator.MoveNextAsync())
        {
            await OnPackageReceived(enumerator.Current);
        }
    }

    /// <summary>
    /// 处理接收到的数据包
    /// </summary>
    /// <param name="package">接收到的数据包</param>
    protected virtual async ValueTask OnPackageReceived(TReceivePackage package)
    {
        var handler = PackageHandler;

        try
        {
            await handler.Invoke(this, package);
        }
        catch (Exception e)
        {
            OnError("Unhandled exception happened in PackageHandler.", e);
        }
    }

    /// <summary>
    /// 连接关闭时的处理方法
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnConnectionClosed(object sender, EventArgs e)
    {
        Connection.Closed -= OnConnectionClosed;
        OnClosed(this, e);
    }

    /// <summary>
    /// 处理关闭事件
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    protected virtual void OnClosed(object sender, EventArgs e)
    {
        var handler = Closed;

        if (handler != null)
        {
            if (Interlocked.CompareExchange(ref Closed, null, handler) == handler)
            {
                handler.Invoke(sender, e);
            }
        }
    }

    /// <summary>
    /// 处理错误信息
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="exception">异常实例</param>
    protected virtual void OnError(string message, Exception exception)
    {
        Logger?.LogError(exception, message);
    }

    /// <summary>
    /// 处理错误信息
    /// </summary>
    /// <param name="message">错误消息</param>
    protected virtual void OnError(string message)
    {
        Logger?.LogError(message);
    }

    /// <summary>
    /// 发送数据的接口实现
    /// </summary>
    /// <param name="data">要发送的数据</param>
    ValueTask IEasyClient<TReceivePackage>.SendAsync(ReadOnlyMemory<byte> data)
    {
        return SendAsync(data);
    }

    /// <summary>
    /// 发送数据
    /// </summary>
    /// <param name="data">要发送的数据</param>
    protected virtual async ValueTask SendAsync(ReadOnlyMemory<byte> data)
    {
        await Connection.SendAsync(data);
    }

    /// <summary>
    /// 使用包编码器发送数据包的接口实现
    /// </summary>
    /// <typeparam name="TSendPackage">发送包类型</typeparam>
    /// <param name="packageEncoder">包编码器</param>
    /// <param name="package">要发送的数据包</param>
    ValueTask IEasyClient<TReceivePackage>.SendAsync<TSendPackage>(IPackageEncoder<TSendPackage> packageEncoder, TSendPackage package)
    {
        return SendAsync<TSendPackage>(packageEncoder, package);
    }

    /// <summary>
    /// 使用包编码器发送数据包
    /// </summary>
    /// <typeparam name="TSendPackage">发送包类型</typeparam>
    /// <param name="packageEncoder">包编码器</param>
    /// <param name="package">要发送的数据包</param>
    protected virtual async ValueTask SendAsync<TSendPackage>(IPackageEncoder<TSendPackage> packageEncoder, TSendPackage package)
    {
        await Connection.SendAsync(packageEncoder, package);
    }

    /// <summary>
    /// 连接关闭事件
    /// </summary>
    public event EventHandler Closed;

    /// <summary>
    /// 关闭连接
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    public virtual async ValueTask CloseAsync()
    {
        await Connection.CloseAsync(CloseReason.LocalClosing);
        OnClosed(this, EventArgs.Empty);
    }
}