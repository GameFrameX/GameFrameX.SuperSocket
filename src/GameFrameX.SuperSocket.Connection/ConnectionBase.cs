using System.IO.Pipelines;
using System.Net;
using GameFrameX.SuperSocket.ProtoBase;

namespace GameFrameX.SuperSocket.Connection
{
    /// <summary>
    /// 连接基类，提供基本的连接功能实现
    /// </summary>
    public abstract class ConnectionBase : IConnection
    {
        /// <summary>
        /// 运行连接并处理数据包
        /// </summary>
        /// <typeparam name="TPackageInfo">数据包类型</typeparam>
        /// <param name="pipelineFilter">管道过滤器</param>
        /// <returns>异步数据包枚举器</returns>
        public abstract IAsyncEnumerable<TPackageInfo> RunAsync<TPackageInfo>(IPipelineFilter<TPackageInfo> pipelineFilter);

        /// <summary>
        /// 发送字节数组数据
        /// </summary>
        /// <param name="buffer">要发送的数据缓冲区</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        public abstract ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送指定类型的数据包
        /// </summary>
        /// <typeparam name="TPackage">数据包类型</typeparam>
        /// <param name="packageEncoder">数据包编码器</param>
        /// <param name="package">要发送的数据包</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        public abstract ValueTask SendAsync<TPackage>(IPackageEncoder<TPackage> packageEncoder, TPackage package, CancellationToken cancellationToken = default);

        /// <summary>
        /// 通过管道写入器发送数据
        /// </summary>
        /// <param name="write">写入操作委托</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        public abstract ValueTask SendAsync(Action<PipeWriter> write, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取连接是否已关闭
        /// </summary>
        public bool IsClosed { get; private set; }

        /// <summary>
        /// 获取远程终结点
        /// </summary>
        public EndPoint RemoteEndPoint { get; protected set; }

        /// <summary>
        /// 获取本地终结点
        /// </summary>
        public EndPoint LocalEndPoint { get; protected set; }

        /// <summary>
        /// 获取连接关闭原因
        /// </summary>
        public CloseReason? CloseReason { get; protected set; }

        /// <summary>
        /// 获取最后活动时间
        /// </summary>
        public DateTimeOffset LastActiveTime { get; protected set; } = DateTimeOffset.Now;

        /// <summary>
        /// 获取连接取消令牌
        /// </summary>
        public CancellationToken ConnectionToken { get; protected set; }

        /// <summary>
        /// 处理连接关闭事件
        /// </summary>
        protected virtual void OnClosed()
        {
            IsClosed = true;

            var closed = Closed;

            if (closed == null)
                return;

            if (Interlocked.CompareExchange(ref Closed, null, closed) != closed)
                return;

            var closeReason = CloseReason.HasValue ? CloseReason.Value : Connection.CloseReason.Unknown;

            closed.Invoke(this, new CloseEventArgs(closeReason));
        }

        /// <summary>
        /// 连接关闭事件
        /// </summary>
        public event EventHandler<CloseEventArgs> Closed;

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <param name="closeReason">关闭原因</param>
        /// <returns>异步任务</returns>
        public abstract ValueTask CloseAsync(CloseReason closeReason);

        /// <summary>
        /// 分离连接
        /// </summary>
        /// <returns>异步任务</returns>
        public abstract ValueTask DetachAsync();
    }
}