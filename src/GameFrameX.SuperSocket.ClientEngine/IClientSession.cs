using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using GameFrameX.SuperSocket.Server.Abstractions.Session;

namespace GameFrameX.SuperSocket.ClientEngine
{
    /// <summary>
    /// 客户端会话接口
    /// </summary>
    public interface IClientSession : IGameAppSession
    {
        /// <summary>
        /// 获取底层Socket对象
        /// </summary>
        Socket Socket { get; }

        /// <summary>
        /// 获取或设置代理连接器
        /// </summary>
        IProxyConnector Proxy { get; set; }

        /// <summary>
        /// 获取或设置接收缓冲区大小
        /// </summary>
        int ReceiveBufferSize { get; set; }

        /// <summary>
        /// 获取或设置发送队列大小
        /// </summary>
        int SendingQueueSize { get; set; }

        /// <summary>
        /// 连接到远程终结点
        /// </summary>
        /// <param name="remoteEndPoint">远程终结点</param>
        void Connect(EndPoint remoteEndPoint);

        /// <summary>
        /// 发送数据片段
        /// </summary>
        /// <param name="segment">要发送的数据片段</param>
        void Send(ArraySegment<byte> segment);

        /// <summary>
        /// 发送多个数据片段
        /// </summary>
        /// <param name="segments">要发送的数据片段列表</param>
        void Send(IList<ArraySegment<byte>> segments);

        /// <summary>
        /// 发送字节数组数据
        /// </summary>
        /// <param name="data">要发送的数据</param>
        /// <param name="offset">起始偏移量</param>
        /// <param name="length">发送长度</param>
        void Send(byte[] data, int offset, int length);

        /// <summary>
        /// 尝试发送数据片段
        /// </summary>
        /// <param name="segment">要发送的数据片段</param>
        /// <returns>发送是否成功</returns>
        bool TrySend(ArraySegment<byte> segment);

        /// <summary>
        /// 尝试发送多个数据片段
        /// </summary>
        /// <param name="segments">要发送的数据片段列表</param>
        /// <returns>发送是否成功</returns>
        bool TrySend(IList<ArraySegment<byte>> segments);

        /// <summary>
        /// 关闭连接
        /// </summary>
        void Close();

        /// <summary>
        /// 连接成功事件
        /// </summary>
        event EventHandler Connected;

        /// <summary>
        /// 连接关闭事件
        /// </summary>
        event EventHandler Closed;

        /// <summary>
        /// 错误发生事件
        /// </summary>
        event EventHandler<ErrorEventArgs> Error;

        /// <summary>
        /// 数据接收事件
        /// </summary>
        event EventHandler<DataEventArgs> DataReceived;
    }
}