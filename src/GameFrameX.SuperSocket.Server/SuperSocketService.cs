using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Primitives;
using GameFrameX.SuperSocket.ProtoBase;
using GameFrameX.SuperSocket.ProtoBase.ProxyProtocol;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;
using GameFrameX.SuperSocket.Server.Abstractions.Middleware;
using GameFrameX.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameFrameX.SuperSocket.Server
{
    /// <summary>
    /// Represents a SuperSocket service that handles connections and sessions.
    /// </summary>
    /// <typeparam name="TReceivePackageInfo">The type of the package information received.</typeparam>
    public class SuperSocketService<TReceivePackageInfo> : ISuperSocketHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Gets the service provider for dependency injection.
        /// </summary>
        public IServiceProvider ServiceProvider
        {
            get { return _serviceProvider; }
        }

        /// <summary>
        /// Gets the server options for configuration.
        /// </summary>
        public ServerOptions Options { get; }
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        internal protected ILogger Logger
        {
            get { return _logger; }
        }

        ILogger ILoggerAccessor.Logger
        {
            get { return _logger; }
        }

        private IPipelineFilterFactory<TReceivePackageInfo> _pipelineFilterFactory;
        private IConnectionListenerFactory _connectionListenerFactory;
        private List<IConnectionListener> _connectionListeners;
        private IPackageHandlingScheduler<TReceivePackageInfo> _packageHandlingScheduler;
        private IPackageHandlingContextAccessor<TReceivePackageInfo> _packageHandlingContextAccessor;

        /// <summary>
        /// Gets the name of the server.
        /// </summary>
        public string Name { get; }

        private int _sessionCount;

        /// <summary>
        /// Gets the current session count.
        /// </summary>
        public int SessionCount => _sessionCount;

        private ISessionFactory _sessionFactory;

        private IMiddleware[] _middlewares;

        protected IMiddleware[] Middlewares
        {
            get { return _middlewares; }
        }

        private ServerState _state = ServerState.None;

        public ServerState State
        {
            get { return _state; }
        }

        public object DataContext { get; set; }

        private SessionHandlers _sessionHandlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="SuperSocketService{TReceivePackageInfo}"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency injection.</param>
        /// <param name="serverOptions">The server options for configuration.</param>
        public SuperSocketService(IServiceProvider serviceProvider, IOptions<ServerOptions> serverOptions)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            if (serverOptions == null)
                throw new ArgumentNullException(nameof(serverOptions));

            Name = serverOptions.Value.Name;
            Options = serverOptions.Value;
            _serviceProvider = serviceProvider;
            _pipelineFilterFactory = GetPipelineFilterFactory();
            _loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            _logger = _loggerFactory.CreateLogger("SuperSocketService");
            _connectionListenerFactory = serviceProvider.GetService<IConnectionListenerFactory>();
            _sessionHandlers = serviceProvider.GetService<SessionHandlers>();
            _sessionFactory = serviceProvider.GetService<ISessionFactory>();
            _packageHandlingContextAccessor = serviceProvider.GetService<IPackageHandlingContextAccessor<TReceivePackageInfo>>();
            InitializeMiddlewares();

            var packageHandler = serviceProvider.GetService<IPackageHandler<TReceivePackageInfo>>()
                ?? _middlewares.OfType<IPackageHandler<TReceivePackageInfo>>().FirstOrDefault();

            if (packageHandler == null)
            {
                Logger.LogWarning("The PackageHandler cannot be found.");
            }
            else
            {
                var errorHandler = serviceProvider.GetService<Func<IAppSession, PackageHandlingException<TReceivePackageInfo>, ValueTask<bool>>>()
                    ?? OnSessionErrorAsync;

                _packageHandlingScheduler = serviceProvider.GetService<IPackageHandlingScheduler<TReceivePackageInfo>>()
                    ?? new SerialPackageHandlingScheduler<TReceivePackageInfo>();
                _packageHandlingScheduler.Initialize(packageHandler, errorHandler);
            }
        }

        protected virtual IPipelineFilterFactory<TReceivePackageInfo> GetPipelineFilterFactory()
        {
            var filterFactory = _serviceProvider.GetRequiredService<IPipelineFilterFactory<TReceivePackageInfo>>();

            if (Options.EnableProxyProtocol)
            {
                filterFactory = new ProxyProtocolPipelineFilterFactory<TReceivePackageInfo>(filterFactory);
            }

            return filterFactory;
        }

        private bool AddConnectionListener(ListenOptions listenOptions, ServerOptions serverOptions)
        {
            var listener = _connectionListenerFactory.CreateConnectionListener(listenOptions, serverOptions, _loggerFactory);
            listener.NewConnectionAccept += OnNewConnectionAccept;

            if (!listener.Start())
            {
                _logger.LogError($"Failed to listen {listener}.");
                return false;
            }

            _logger.LogInformation($"The listener [{listener}] has been started.");
            _connectionListeners.Add(listener);
            return true;
        }

        private Task<bool> StartListenAsync(CancellationToken cancellationToken)
        {
            _connectionListeners = new List<IConnectionListener>();

            var serverOptions = Options;

            if (serverOptions.Listeners != null && serverOptions.Listeners.Any())
            {
                foreach (var l in serverOptions.Listeners)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (!AddConnectionListener(l, serverOptions))
                    {
                        continue;
                    }
                }
            }
            else
            {
                _logger.LogWarning("No listener was defined, so this server only can accept connections from the ActiveConnect.");

                if (!AddConnectionListener(null, serverOptions))
                {
                    _logger.LogError($"Failed to add the connection creator.");
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(_connectionListeners.Any());
        }

        protected virtual ValueTask OnNewConnectionAccept(ListenOptions listenOptions, IConnection connection)
        {
            return AcceptNewConnection(connection);
        }

        private ValueTask AcceptNewConnection(IConnection connection)
        {
            var session = _sessionFactory.Create() as AppSession;
            return HandleSession(session, connection);
        }

        async Task IConnectionRegister.RegisterConnection(object connectionSource)
        {
            var connectionListener = _connectionListeners.FirstOrDefault();
#if NET6_0_OR_GREATER
            using var cts = CancellationTokenSourcePool.Shared.Rent(connectionListener.Options.ConnectionAcceptTimeOut);
#else
            using var cts = new CancellationTokenSource(connectionListener.Options.ConnectionAcceptTimeOut);
#endif
            var connection = await connectionListener.ConnectionFactory.CreateConnection(connectionSource, cts.Token);
            await AcceptNewConnection(connection);
        }

        protected virtual object CreatePipelineContext(IAppSession session)
        {
            return session;
        }

        #region Middlewares

        private void InitializeMiddlewares()
        {
            _middlewares = _serviceProvider.GetServices<IMiddleware>()
                .OrderBy(m => m.Order)
                .ToArray();
        }

        private void ShutdownMiddlewares()
        {
            foreach (var m in _middlewares)
            {
                try
                {
                    m.Shutdown(this);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"The exception was thrown from the middleware {m.GetType().Name} when it is being shutdown.");
                }
            }
        }

        private async ValueTask<bool> RegisterSessionInMiddlewares(IAppSession session)
        {
            var middlewares = _middlewares;

            if (middlewares != null && middlewares.Length > 0)
            {
                for (var i = 0; i < middlewares.Length; i++)
                {
                    var middleware = middlewares[i];

                    if (!await middleware.RegisterSession(session))
                    {
                        _logger.LogWarning($"A session from {session.RemoteEndPoint} was rejected by the middleware {middleware.GetType().Name}.");
                        return false;
                    }
                }
            }

            return true;
        }

        private async ValueTask UnRegisterSessionFromMiddlewares(IAppSession session)
        {
            var middlewares = _middlewares;

            if (middlewares != null && middlewares.Length > 0)
            {
                for (var i = 0; i < middlewares.Length; i++)
                {
                    var middleware = middlewares[i];

                    try
                    {
                        if (!await middleware.UnRegisterSession(session))
                        {
                            _logger.LogWarning($"The session from {session.RemoteEndPoint} was failed to be unregistered from the middleware {middleware.GetType().Name}.");
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"An unhandled exception occured when the session from {session.RemoteEndPoint} was being unregistered from the middleware {nameof(RegisterSessionInMiddlewares)}.");
                    }
                }
            }
        }

        #endregion

        private async ValueTask<bool> InitializeSession(IAppSession session, IConnection connection)
        {
            session.Initialize(this, connection);

            var middlewares = _middlewares;

            try
            {
                if (!await RegisterSessionInMiddlewares(session))
                    return false;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"An unhandled exception occured in {nameof(RegisterSessionInMiddlewares)}.");
                return false;
            }

            connection.Closed += (s, e) => OnConnectionClosed(session, e);
            return true;
        }


        protected virtual ValueTask OnSessionConnectedAsync(IAppSession session)
        {
            var connectedHandler = _sessionHandlers?.Connected;

            if (connectedHandler != null)
                return connectedHandler.Invoke(session);

            return new ValueTask();
        }

        private void OnConnectionClosed(IAppSession session, CloseEventArgs e)
        {
            FireSessionClosedEvent(session as AppSession, e.Reason).DoNotAwait();
        }

        protected virtual ValueTask OnSessionClosedAsync(IAppSession session, CloseEventArgs e)
        {
            var closedHandler = _sessionHandlers?.Closed;

            if (closedHandler != null)
                return closedHandler.Invoke(session, e);

#if NETSTANDARD2_1
            return GetCompletedTask();
#else
            return ValueTask.CompletedTask;
#endif
        }

        protected virtual async ValueTask FireSessionConnectedEvent(AppSession session)
        {
            if (session is IHandshakeRequiredSession handshakeSession)
            {
                if (!handshakeSession.Handshaked)
                    return;
            }

            _logger.LogInformation($"A new session connected: {session.SessionID}");

            try
            {
                Interlocked.Increment(ref _sessionCount);
                await session.FireSessionConnectedAsync();
                await OnSessionConnectedAsync(session);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "There is one exception thrown from the event handler of SessionConnected.");
            }
        }

        protected virtual async ValueTask FireSessionClosedEvent(AppSession session, CloseReason reason)
        {
            if (session is IHandshakeRequiredSession handshakeSession)
            {
                if (!handshakeSession.Handshaked)
                    return;
            }

            await UnRegisterSessionFromMiddlewares(session);

            _logger.LogInformation($"The session disconnected: {session.SessionID} ({reason})");

            try
            {
                Interlocked.Decrement(ref _sessionCount);

                var closeEventArgs = new CloseEventArgs(reason);
                await session.FireSessionClosedAsync(closeEventArgs);
                await OnSessionClosedAsync(session, closeEventArgs);
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "There is one exception thrown from the event of OnSessionClosed.");
            }
        }

        ValueTask ISessionEventHost.HandleSessionConnectedEvent(IAppSession session)
        {
            return FireSessionConnectedEvent((AppSession)session);
        }

        ValueTask ISessionEventHost.HandleSessionClosedEvent(IAppSession session, CloseReason reason)
        {
            return FireSessionClosedEvent((AppSession)session, reason);
        }

        private async ValueTask HandleSession(AppSession session, IConnection connection)
        {
            if (!await InitializeSession(session, connection))
                return;

            try
            {
                var pipelineFilter = _pipelineFilterFactory.Create(connection);
                pipelineFilter.Context = CreatePipelineContext(session);

                var packageStream = connection.RunAsync<TReceivePackageInfo>(pipelineFilter);

                await FireSessionConnectedEvent(session);

                var packageHandlingScheduler = _packageHandlingScheduler;

#if NET6_0_OR_GREATER
                using var cancellationTokenSource = GetPackageHandlingCancellationTokenSource(connection.ConnectionToken);
#endif
                ValueTask prevPackageHandlingTask = ValueTask.CompletedTask;

                await foreach (var p in packageStream)
                {
                    if (_packageHandlingContextAccessor != null)
                    {
                        _packageHandlingContextAccessor.PackageHandlingContext = new PackageHandlingContext<IAppSession, TReceivePackageInfo>(session, p);
                    }

#if !NET6_0_OR_GREATER
                    using var cancellationTokenSource = GetPackageHandlingCancellationTokenSource(connection.ConnectionToken);
#endif
                    if (prevPackageHandlingTask != ValueTask.CompletedTask)
                    {
                        await prevPackageHandlingTask;
                    }

                    prevPackageHandlingTask = packageHandlingScheduler.HandlePackage(session, p, cancellationTokenSource.Token);

#if NET6_0_OR_GREATER
                    cancellationTokenSource.TryReset();
#endif
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to handle the session {session.SessionID}.");
            }
        }

        protected virtual CancellationTokenSource GetPackageHandlingCancellationTokenSource(CancellationToken cancellationToken)
        {
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(Options.PackageHandlingTimeOut));
            return cancellationTokenSource;
        }

        protected virtual ValueTask<bool> OnSessionErrorAsync(IAppSession session, PackageHandlingException<TReceivePackageInfo> exception)
        {
            _logger.LogError(exception, $"Session[{session.SessionID}]: session exception.");
            return new ValueTask<bool>(true);
        }

        /// <summary>
        /// Starts the SuperSocket service asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task<bool> StartAsync(CancellationToken cancellationToken)
        {
            var state = _state;

            if (state != ServerState.None && state != ServerState.Stopped && state != ServerState.Failed)
            {
                throw new InvalidOperationException($"The server cannot be started right now, because its state is {state}.");
            }

            _state = ServerState.Starting;

            foreach (var m in _middlewares)
            {
                m.Start(this);
            }

            if (!await StartListenAsync(cancellationToken))
            {
                _state = ServerState.Failed;
                _logger.LogError("Failed to start any listener.");
                return false;
            }

            _state = ServerState.Started;

            try
            {
                await OnStartedAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "There is one exception thrown from the method OnStartedAsync().");
            }

            return true;
        }

        protected virtual ValueTask OnStartedAsync()
        {
#if NETSTANDARD2_1
            return GetCompletedTask();
#else
            return ValueTask.CompletedTask;
#endif
        }

        protected virtual ValueTask OnStopAsync()
        {
#if NETSTANDARD2_1
            return GetCompletedTask();
#else
            return ValueTask.CompletedTask;
#endif
        }

        private async Task StopListener(IConnectionListener listener)
        {
            await listener.StopAsync().ConfigureAwait(false);
            _logger.LogInformation($"The listener [{listener}] has been stopped.");
        }

        /// <summary>
        /// Stops the SuperSocket service asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var state = _state;

            if (state != ServerState.Started)
            {
                throw new InvalidOperationException($"The server cannot be stopped right now, because its state is {state}.");
            }

            _state = ServerState.Stopping;

            var tasks = _connectionListeners.Where(l => l.IsRunning).Select(l => StopListener(l))
                .Union(new Task[] { Task.Run(ShutdownMiddlewares) });

            await Task.WhenAll(tasks).ConfigureAwait(false);

            try
            {
                await OnStopAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "There is an exception thrown from the method OnStopAsync().");
            }

            _state = ServerState.Stopped;
        }

        async Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            if (!await StartAsync(cancellationToken))
            {
                throw new Exception("Failed to start the server.");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        ValueTask IAsyncDisposable.DisposeAsync() => DisposeAsync(true);

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        if (_state == ServerState.Started)
                        {
                            await StopAsync(CancellationToken.None);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to stop the server");
                    }

                    var connectionListeners = _connectionListeners;

                    if (connectionListeners != null && connectionListeners.Any())
                    {
                        foreach (var listener in connectionListeners)
                        {
                            listener.Dispose();
                        }
                    }
                }

                disposedValue = true;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            DisposeAsync(disposing).GetAwaiter().GetResult();
        }

        void IDisposable.Dispose()
        {
            DisposeAsync(true).GetAwaiter().GetResult();
        }

        #endregion
    }
}