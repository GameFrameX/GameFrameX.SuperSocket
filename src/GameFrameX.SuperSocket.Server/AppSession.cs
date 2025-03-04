using System.Net;
using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Primitives;
using GameFrameX.SuperSocket.ProtoBase;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.Logging;

namespace GameFrameX.SuperSocket.Server
{
    /// <summary>
    /// 应用会话类，实现了IAppSession、ILogger和ILoggerAccessor接口
    /// </summary>
    public class AppSession : IAppSession, ILogger, ILoggerAccessor
    {
        private IConnection _connection;

        /// <summary>
        /// 获取连接对象
        /// </summary>
        protected internal IConnection Connection
        {
            get { return _connection; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public AppSession()
        {
        }

        /// <summary>
        /// 初始化会话
        /// </summary>
        /// <param name="server">服务器信息</param>
        /// <param name="connection">连接对象</param>
        void IAppSession.Initialize(IServerInfo server, IConnection connection)
        {
            if (connection is IConnectionWithSessionIdentifier connectionWithSessionIdentifier)
                SessionID = connectionWithSessionIdentifier.SessionIdentifier;
            else
                SessionID = Guid.NewGuid().ToString();

            Server = server;
            StartTime = DateTimeOffset.Now;
            _connection = connection;
            State = SessionState.Initialized;
        }

        /// <summary>
        /// 会话ID
        /// </summary>
        public string SessionID { get; private set; }

        /// <summary>
        /// 异步发送字节数组数据
        /// </summary>
        /// <param name="data">要发送的数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        public virtual ValueTask SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            return _connection.SendAsync(data, cancellationToken);
        }

        /// <summary>
        /// 异步发送只读内存数据
        /// </summary>
        /// <param name="data">要发送的数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        public virtual ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            return _connection.SendAsync(data, cancellationToken);
        }

        /// <summary>
        /// 会话开始时间
        /// </summary>
        public DateTimeOffset StartTime { get; private set; }

        /// <summary>
        /// 会话状态
        /// </summary>
        public SessionState State { get; private set; } = SessionState.None;

        /// <summary>
        /// 会话是否已连接
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (_connection?.CloseReason.HasValue != null)
                {
                    return false;
                }

                return State == SessionState.Connected;
            }
        }

        /// <summary>
        /// 服务器信息
        /// </summary>
        public IServerInfo Server { get; private set; }

        /// <summary>
        /// 连接对象
        /// </summary>
        IConnection IAppSession.Connection
        {
            get { return _connection; }
        }

        /// <summary>
        /// 数据上下文
        /// </summary>
        public object DataContext { get; set; }

        /// <summary>
        /// 远程终结点
        /// </summary>
        public EndPoint RemoteEndPoint
        {
            get { return _connection?.RemoteEndPoint; }
        }

        /// <summary>
        /// 本地终结点
        /// </summary>
        public EndPoint LocalEndPoint
        {
            get { return _connection?.LocalEndPoint; }
        }

        /// <summary>
        /// 最后活动时间
        /// </summary>
        public DateTimeOffset LastActiveTime
        {
            get { return _connection?.LastActiveTime ?? DateTimeOffset.MinValue; }
        }

        /// <summary>
        /// 连接事件
        /// </summary>
        public event AsyncEventHandler Connected;

        /// <summary>
        /// 关闭事件
        /// </summary>
        public event AsyncEventHandler<CloseEventArgs> Closed;

        private Dictionary<object, object> _items;

        /// <summary>
        /// 索引器，用于存储和获取会话相关的数据
        /// </summary>
        /// <param name="name">键名</param>
        public object this[object name]
        {
            get
            {
                var items = _items;

                if (items == null)
                    return null;

                object value;

                if (items.TryGetValue(name, out value))
                {
                    return value;
                }

                return null;
            }

            set
            {
                lock (this)
                {
                    var items = _items;

                    if (items == null)
                        items = _items = new Dictionary<object, object>();

                    items[name] = value;
                }
            }
        }

        /// <summary>
        /// 会话关闭时的虚拟方法
        /// </summary>
        /// <param name="e">关闭事件参数</param>
        protected virtual ValueTask OnSessionClosedAsync(CloseEventArgs e)
        {
            return new ValueTask();
        }

        /// <summary>
        /// 触发会话关闭事件
        /// </summary>
        /// <param name="e">关闭事件参数</param>
        internal async ValueTask FireSessionClosedAsync(CloseEventArgs e)
        {
            State = SessionState.Closed;

            await OnSessionClosedAsync(e);

            var closeEventHandler = Closed;

            if (closeEventHandler == null)
                return;

            await closeEventHandler.Invoke(this, e);
        }

        /// <summary>
        /// 会话连接时的虚拟方法
        /// </summary>
        protected virtual ValueTask OnSessionConnectedAsync()
        {
            return new ValueTask();
        }

        /// <summary>
        /// 触发会话连接事件
        /// </summary>
        internal async ValueTask FireSessionConnectedAsync()
        {
            State = SessionState.Connected;

            await OnSessionConnectedAsync();

            var connectedEventHandler = Connected;

            if (connectedEventHandler == null)
                return;

            await connectedEventHandler.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 发送数据包
        /// </summary>
        /// <typeparam name="TPackage">数据包类型</typeparam>
        /// <param name="packageEncoder">数据包编码器</param>
        /// <param name="package">数据包</param>
        /// <param name="cancellationToken">取消令牌</param>
        ValueTask IAppSession.SendAsync<TPackage>(IPackageEncoder<TPackage> packageEncoder, TPackage package, CancellationToken cancellationToken)
        {
            return _connection.SendAsync(packageEncoder, package, cancellationToken);
        }

        /// <summary>
        /// 重置会话
        /// </summary>
        void IAppSession.Reset()
        {
            ClearEvent(ref Connected);
            ClearEvent(ref Closed);
            _items?.Clear();
            State = SessionState.None;
            _connection = null;
            DataContext = null;
            StartTime = default(DateTimeOffset);
            Server = null;

            Reset();
        }

        /// <summary>
        /// 重置会话的虚拟方法
        /// </summary>
        protected virtual void Reset()
        {
        }

        /// <summary>
        /// 清除事件处理程序
        /// </summary>
        /// <typeparam name="TEventHandler">事件处理程序类型</typeparam>
        /// <param name="sessionEvent">事件引用</param>
        private void ClearEvent<TEventHandler>(ref TEventHandler sessionEvent)
            where TEventHandler : Delegate
        {
            if (sessionEvent == null)
            {
                return;
            }

            foreach (var handler in sessionEvent.GetInvocationList())
            {
                sessionEvent = Delegate.Remove(sessionEvent, handler) as TEventHandler;
            }
        }

        /// <summary>
        /// 异步关闭会话
        /// </summary>
        public virtual async ValueTask CloseAsync()
        {
            await CloseAsync(CloseReason.LocalClosing);
        }

        /// <summary>
        /// 使用指定原因异步关闭会话
        /// </summary>
        /// <param name="reason">关闭原因</param>
        public virtual async ValueTask CloseAsync(CloseReason reason)
        {
            var connection = Connection;
            State = SessionState.Closed;
            if (connection == null)
            {
                return;
            }

            try
            {
                await connection.CloseAsync(reason);
            }
            catch
            {
            }
        }

        #region ILogger

        /// <summary>
        /// 获取日志记录器
        /// </summary>
        ILogger GetLogger()
        {
            return (Server as ILoggerAccessor).Logger;
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            GetLogger().Log<TState>(logLevel, eventId, state, exception, (s, e) => { return $"Session[{this.SessionID}]: {formatter(s, e)}"; });
        }

        /// <summary>
        /// 检查日志级别是否启用
        /// </summary>
        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return GetLogger().IsEnabled(logLevel);
        }

        /// <summary>
        /// 开始日志范围
        /// </summary>
        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            return GetLogger().BeginScope<TState>(state);
        }

        /// <summary>
        /// 日志记录器属性
        /// </summary>
        public ILogger Logger => this as ILogger;

        #endregion
    }
}