using System.Collections.Concurrent;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Middleware;
using GameFrameX.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.Options;

namespace GameFrameX.SuperSocket.WebSocket.Server;

/// <summary>
/// WebSocket 服务器中间件接口
/// </summary>
internal interface IWebSocketServerMiddleware
{
    /// <summary>
    /// 获取打开握手等待队列长度
    /// </summary>
    int OpenHandshakePendingQueueLength { get; }

    /// <summary>
    /// 获取关闭握手等待队列长度
    /// </summary>
    int CloseHandshakePendingQueueLength { get; }

    /// <summary>
    /// 处理会话握手完成事件
    /// </summary>
    ValueTask HandleSessionHandshakeCompleted(WebSocketSession session);
}

/// <summary>
/// WebSocket 服务器中间件实现类
/// </summary>
internal class WebSocketServerMiddleware : MiddlewareBase, IWebSocketServerMiddleware
{
    private readonly HandshakeOptions _options;

    private Timer _checkingTimer;

    private readonly ConcurrentQueue<WebSocketSession> _closeHandshakePendingQueue = new();
    private readonly ConcurrentQueue<WebSocketSession> _openHandshakePendingQueue = new();

    private IMiddleware _sessionContainerMiddleware;

    private ISessionEventHost _sessionEventHost;

    public WebSocketServerMiddleware(IOptions<HandshakeOptions> handshakeOptions)
    {
        var options = handshakeOptions.Value;

        if (options == null)
            options = new HandshakeOptions();

        _options = options;
    }

    public int OpenHandshakePendingQueueLength => _openHandshakePendingQueue.Count;

    public int CloseHandshakePendingQueueLength => _closeHandshakePendingQueue.Count;

    public ValueTask HandleSessionHandshakeCompleted(WebSocketSession session)
    {
        session.CloseHandshakeStarted += OnCloseHandshakeStarted;
        _sessionContainerMiddleware?.RegisterSession(session);
        return _sessionEventHost.HandleSessionConnectedEvent(session);
    }

    public override void Start(IServer server)
    {
        _sessionContainerMiddleware = server.GetSessionContainer() as IMiddleware;
        _sessionEventHost = server as ISessionEventHost;
        _checkingTimer = new Timer(HandshakePendingQueueCheckingCallback, null, _options.CheckingInterval * 1000, _options.CheckingInterval * 1000); // hardcode to 1 minute for now
    }

    public override void Shutdown(IServer server)
    {
        _sessionContainerMiddleware = null;
        var checkTimer = _checkingTimer;
        if (checkTimer == null) return;

        if (Interlocked.CompareExchange(ref _checkingTimer, null, checkTimer) == checkTimer)
        {
            checkTimer.Change(Timeout.Infinite, Timeout.Infinite);
            checkTimer.Dispose();
        }
    }

    public override ValueTask<bool> RegisterSession(IAppSession session)
    {
        var websocketSession = session as WebSocketSession;
        _openHandshakePendingQueue.Enqueue(websocketSession);
        return new ValueTask<bool>(true);
    }

    private void OnCloseHandshakeStarted(object sender, EventArgs e)
    {
        var session = sender as WebSocketSession;
        session.CloseHandshakeStarted -= OnCloseHandshakeStarted;
        _closeHandshakePendingQueue.Enqueue(session);
    }

    private void HandshakePendingQueueCheckingCallback(object state)
    {
        _checkingTimer.Change(Timeout.Infinite, Timeout.Infinite);

        var openHandshakeTimeTask = Task.Run(() =>
        {
            while (true)
            {
                WebSocketSession session;

                if (!_openHandshakePendingQueue.TryPeek(out session))
                    break;

                if (session.Handshaked || session.State == SessionState.Closed || (session is IAppSession appSession && appSession.Connection.IsClosed))
                {
                    //Handshaked or not connected
                    _openHandshakePendingQueue.TryDequeue(out session);
                    continue;
                }

                if (DateTime.Now < session.StartTime.AddSeconds(_options.OpenHandshakeTimeOut))
                    break;

                //Timeout, dequeue and then close
                _openHandshakePendingQueue.TryDequeue(out session);
                session.CloseWithoutHandshake();
            }
        });

        var closeHandshakeTimeTask = Task.Run(() =>
        {
            while (true)
            {
                WebSocketSession session;

                if (!_closeHandshakePendingQueue.TryPeek(out session))
                    break;

                if (session.State == SessionState.Closed)
                {
                    //the session has been closed
                    _closeHandshakePendingQueue.TryDequeue(out session);
                    continue;
                }

                if (DateTime.Now < session.CloseHandshakeStartTime.AddSeconds(_options.CloseHandshakeTimeOut))
                    break;

                //Timeout, dequeue and then close
                _closeHandshakePendingQueue.TryDequeue(out session);
                //Needn't send closing handshake again
                session.CloseWithoutHandshake();
            }
        });

        Task.WhenAll(openHandshakeTimeTask, closeHandshakeTimeTask);

        _checkingTimer?.Change(_options.CheckingInterval * 1000, _options.CheckingInterval * 1000);
    }
}