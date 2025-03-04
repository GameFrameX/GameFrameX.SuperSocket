using System.Net;
using GameFrameX.SuperSocket.ProtoBase;

namespace GameFrameX.SuperSocket.Client
{
    /// <summary>
    /// 支持发送和接收不同包类型的简易客户端接口
    /// </summary>
    /// <typeparam name="TReceivePackage">接收包类型</typeparam>
    /// <typeparam name="TSendPackage">发送包类型</typeparam>
    public interface IEasyClient<TReceivePackage, TSendPackage> : IEasyClient<TReceivePackage>
        where TReceivePackage : class
    {
        /// <summary>
        /// 异步发送数据包
        /// </summary>
        /// <param name="package">要发送的数据包</param>
        /// <returns>表示异步操作的任务</returns>
        ValueTask SendAsync(TSendPackage package);
    }

    /// <summary>
    /// 简易客户端接口
    /// </summary>
    /// <typeparam name="TReceivePackage">接收包类型</typeparam>
    public interface IEasyClient<TReceivePackage>
        where TReceivePackage : class
    {
        /// <summary>
        /// 异步连接到远程终端
        /// </summary>
        /// <param name="remoteEndPoint">远程终端点</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>连接是否成功</returns>
        ValueTask<bool> ConnectAsync(EndPoint remoteEndPoint, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步接收数据包
        /// </summary>
        /// <returns>接收到的数据包</returns>
        ValueTask<TReceivePackage> ReceiveAsync();

        /// <summary>
        /// 本地终端点
        /// </summary>
        IPEndPoint LocalEndPoint { get; set; }

        /// <summary>
        /// 安全选项
        /// </summary>
        SecurityOptions Security { get; set; }

        /// <summary>
        /// 开始接收数据
        /// </summary>
        void StartReceive();

        /// <summary>
        /// 异步发送字节数据
        /// </summary>
        /// <param name="data">要发送的字节数据</param>
        /// <returns>表示异步操作的任务</returns>
        ValueTask SendAsync(ReadOnlyMemory<byte> data);

        /// <summary>
        /// 使用指定的编码器异步发送数据包
        /// </summary>
        /// <typeparam name="TSendPackage">发送包类型</typeparam>
        /// <param name="packageEncoder">包编码器</param>
        /// <param name="package">要发送的数据包</param>
        /// <returns>表示异步操作的任务</returns>
        ValueTask SendAsync<TSendPackage>(IPackageEncoder<TSendPackage> packageEncoder, TSendPackage package);

        /// <summary>
        /// 连接关闭事件
        /// </summary>
        event EventHandler Closed;

        /// <summary>
        /// 数据包处理事件
        /// </summary>
        event PackageHandler<TReceivePackage> PackageHandler;

        /// <summary>
        /// 异步关闭连接
        /// </summary>
        /// <returns>表示异步操作的任务</returns>
        ValueTask CloseAsync();
    }
}