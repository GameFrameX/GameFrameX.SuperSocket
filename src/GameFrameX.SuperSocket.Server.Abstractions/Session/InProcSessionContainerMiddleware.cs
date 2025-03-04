using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Server.Abstractions.Middleware;

namespace GameFrameX.SuperSocket.Server.Abstractions.Session
{
    /// <summary>
    /// 进程内会话容器中间件，用于管理和存储应用会话
    /// </summary>
    public class InProcSessionContainerMiddleware : MiddlewareBase, ISessionContainer
    {
        /// <summary>
        /// 存储会话的并发字典
        /// </summary>
        private ConcurrentDictionary<string, IAppSession> _sessions;

        /// <summary>
        /// 初始化进程内会话容器中间件的新实例
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        public InProcSessionContainerMiddleware(IServiceProvider serviceProvider)
        {
            Order = int.MaxValue; // make sure it is the last middleware
            _sessions = new ConcurrentDictionary<string, IAppSession>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 注册一个新的会话
        /// </summary>
        /// <param name="session">要注册的会话实例</param>
        /// <returns>注册是否成功的任务</returns>
        public override ValueTask<bool> RegisterSession(IAppSession session)
        {
            if (session is IHandshakeRequiredSession handshakeSession)
            {
                if (!handshakeSession.Handshaked)
                    return new ValueTask<bool>(true);
            }

            _sessions.TryAdd(session.SessionID, session);
            return new ValueTask<bool>(true);
        }

        /// <summary>
        /// 注销一个已存在的会话
        /// </summary>
        /// <param name="session">要注销的会话实例</param>
        /// <returns>注销是否成功的任务</returns>
        public override ValueTask<bool> UnRegisterSession(IAppSession session)
        {
            _sessions.TryRemove(session.SessionID, out IAppSession removedSession);
            return new ValueTask<bool>(true);
        }

        /// <summary>
        /// 根据会话ID获取对应的会话实例
        /// </summary>
        /// <param name="sessionID">会话ID</param>
        /// <returns>找到的会话实例，如果未找到则返回null</returns>
        public IAppSession GetSessionByID(string sessionID)
        {
            _sessions.TryGetValue(sessionID, out IAppSession session);
            return session;
        }

        /// <summary>
        /// 获取当前会话总数
        /// </summary>
        /// <returns>会话总数</returns>
        public int GetSessionCount()
        {
            return _sessions.Count;
        }

        /// <summary>
        /// 获取符合指定条件的所有会话
        /// </summary>
        /// <param name="criteria">筛选条件，如果为null则返回所有已连接的会话</param>
        /// <returns>符合条件的会话集合</returns>
        public IEnumerable<IAppSession> GetSessions(Predicate<IAppSession> criteria = null)
        {
            var enumerator = _sessions.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var s = enumerator.Current.Value;

                if (s.State != SessionState.Connected)
                    continue;

                if (criteria == null || criteria(s))
                    yield return s;
            }
        }

        /// <summary>
        /// 获取指定类型且符合条件的所有会话
        /// </summary>
        /// <typeparam name="TAppSession">会话类型</typeparam>
        /// <param name="criteria">筛选条件，如果为null则返回所有已连接的指定类型会话</param>
        /// <returns>符合条件的指定类型会话集合</returns>
        public IEnumerable<TAppSession> GetSessions<TAppSession>(Predicate<TAppSession> criteria = null) where TAppSession : IAppSession
        {
            var enumerator = _sessions.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Value is TAppSession s)
                {
                    if (s.State != SessionState.Connected)
                        continue;

                    if (criteria == null || criteria(s))
                        yield return s;
                }
            }
        }
    }
}