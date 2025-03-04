using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameFrameX.SuperSocket.Server.Abstractions.Session
{
    /// <summary>
    /// 会话容器接口，用于管理和访问应用会话
    /// </summary>
    public interface ISessionContainer
    {
        /// <summary>
        /// 根据会话ID获取对应的应用会话
        /// </summary>
        /// <param name="sessionID">会话ID</param>
        /// <returns>返回对应的应用会话，如果不存在则返回null</returns>
        IAppSession GetSessionByID(string sessionID);

        /// <summary>
        /// 获取当前会话总数
        /// </summary>
        /// <returns>返回当前会话数量</returns>
        int GetSessionCount();

        /// <summary>
        /// 获取符合指定条件的所有会话
        /// </summary>
        /// <param name="criteria">筛选条件，如果为null则返回所有会话</param>
        /// <returns>返回符合条件的会话集合</returns>
        IEnumerable<IAppSession> GetSessions(Predicate<IAppSession> criteria = null);

        /// <summary>
        /// 获取指定类型且符合条件的所有会话
        /// </summary>
        /// <typeparam name="TAppSession">会话类型</typeparam>
        /// <param name="criteria">筛选条件，如果为null则返回所有指定类型的会话</param>
        /// <returns>返回符合条件的指定类型会话集合</returns>
        IEnumerable<TAppSession> GetSessions<TAppSession>(Predicate<TAppSession> criteria = null)
            where TAppSession : IAppSession;
    }

    /// <summary>
    /// 异步会话容器接口，提供异步方式管理和访问应用会话
    /// </summary>
    public interface IAsyncSessionContainer
    {
        /// <summary>
        /// 异步根据会话ID获取对应的应用会话
        /// </summary>
        /// <param name="sessionID">会话ID</param>
        /// <returns>返回对应的应用会话的异步任务，如果不存在则返回null</returns>
        ValueTask<IAppSession> GetSessionByIDAsync(string sessionID);

        /// <summary>
        /// 异步获取当前会话总数
        /// </summary>
        /// <returns>返回包含当前会话数量的异步任务</returns>
        ValueTask<int> GetSessionCountAsync();

        /// <summary>
        /// 异步获取符合指定条件的所有会话
        /// </summary>
        /// <param name="criteria">筛选条件，如果为null则返回所有会话</param>
        /// <returns>返回包含符合条件的会话集合的异步任务</returns>
        ValueTask<IEnumerable<IAppSession>> GetSessionsAsync(Predicate<IAppSession> criteria = null);

        /// <summary>
        /// 异步获取指定类型且符合条件的所有会话
        /// </summary>
        /// <typeparam name="TAppSession">会话类型</typeparam>
        /// <param name="criteria">筛选条件，如果为null则返回所有指定类型的会话</param>
        /// <returns>返回包含符合条件的指定类型会话集合的异步任务</returns>
        ValueTask<IEnumerable<TAppSession>> GetSessionsAsync<TAppSession>(Predicate<TAppSession> criteria = null)
            where TAppSession : IAppSession;
    }
}