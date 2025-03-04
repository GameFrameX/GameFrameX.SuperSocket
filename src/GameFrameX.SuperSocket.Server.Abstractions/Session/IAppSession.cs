using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.ProtoBase;

namespace GameFrameX.SuperSocket.Server.Abstractions.Session
{
    /// <summary>
    /// 应用会话接口，定义了会话的基本属性和操作
    /// </summary>
    public interface IAppSession : IGameAppSession
    {
        /// <summary>
        /// 获取会话的开始时间
        /// </summary>
        DateTimeOffset StartTime { get; }

        /// <summary>
        /// 获取会话的最后活动时间
        /// </summary>
        DateTimeOffset LastActiveTime { get; }

        /// <summary>
        /// 获取会话的连接对象
        /// </summary>
        IConnection Connection { get; }

        /// <summary>
        /// 获取远程终端点
        /// </summary>
        EndPoint RemoteEndPoint { get; }

        /// <summary>
        /// 获取本地终端点
        /// </summary>
        EndPoint LocalEndPoint { get; }

        /// <summary>
        /// 异步发送数据包
        /// </summary>
        /// <typeparam name="TPackage">数据包类型</typeparam>
        /// <param name="packageEncoder">数据包编码器</param>
        /// <param name="package">要发送的数据包</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示异步操作的任务</returns>
        ValueTask SendAsync<TPackage>(IPackageEncoder<TPackage> packageEncoder, TPackage package, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步关闭会话
        /// </summary>
        /// <param name="reason">关闭原因</param>
        /// <returns>表示异步操作的任务</returns>
        ValueTask CloseAsync(CloseReason reason);

        /// <summary>
        /// 获取服务器信息
        /// </summary>
        IServerInfo Server { get; }

        /// <summary>
        /// 连接建立时触发的事件
        /// </summary>
        event AsyncEventHandler Connected;

        /// <summary>
        /// 连接关闭时触发的事件
        /// </summary>
        event AsyncEventHandler<CloseEventArgs> Closed;

        /// <summary>
        /// 获取或设置数据上下文
        /// </summary>
        object DataContext { get; set; }

        /// <summary>
        /// 初始化会话
        /// </summary>
        /// <param name="server">服务器信息</param>
        /// <param name="connection">连接对象</param>
        void Initialize(IServerInfo server, IConnection connection);

        /// <summary>
        /// 获取或设置会话数据
        /// </summary>
        /// <param name="name">数据键名</param>
        /// <returns>会话数据</returns>
        object this[object name] { get; set; }

        /// <summary>
        /// 获取会话状态
        /// </summary>
        SessionState State { get; }

        /// <summary>
        /// 重置会话状态
        /// </summary>
        void Reset();
    }
}