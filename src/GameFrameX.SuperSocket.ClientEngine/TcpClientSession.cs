using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GameFrameX.SuperSocket.ClientEngine
{
    /// <summary>
    /// TCP客户端会话的抽象基类
    /// </summary>
    public abstract class TcpClientSession : ClientSession
    {
        /// <summary>
        /// 获取或设置主机名
        /// </summary>
        protected string HostName { get; private set; }

        /// <summary>
        /// 初始化TCP客户端会话
        /// </summary>
        public TcpClientSession()
        {
        }

        /// <summary>
        /// 获取或设置本地终结点
        /// </summary>
        /// <exception cref="Exception">当连接已启动或已连接时,不能设置本地终结点</exception>
        public override EndPoint LocalEndPoint
        {
            get { return base.LocalEndPoint; }
            set
            {
                if (this.m_InConnecting || base.IsConnected)
                {
                    throw new Exception("You cannot set LocalEdnPoint after you start the connection.");
                }

                base.LocalEndPoint = value;
            }
        }

        /// <summary>
        /// 获取或设置接收缓冲区大小
        /// </summary>
        /// <exception cref="Exception">当socket已连接时,不能设置接收缓冲区大小</exception>
        public override int ReceiveBufferSize
        {
            get { return base.ReceiveBufferSize; }
            set
            {
                if (base.Buffer.Array != null)
                {
                    throw new Exception("ReceiveBufferSize cannot be set after the socket has been connected!");
                }

                base.ReceiveBufferSize = value;
            }
        }

        /// <summary>
        /// 判断异常是否可以忽略
        /// </summary>
        /// <param name="e">要判断的异常</param>
        /// <returns>如果是ObjectDisposedException或NullReferenceException返回true,否则返回false</returns>
        protected virtual bool IsIgnorableException(Exception e)
        {
            return e is ObjectDisposedException || e is NullReferenceException;
        }

        /// <summary>
        /// 判断Socket错误码是否可以忽略
        /// </summary>
        /// <param name="errorCode">Socket错误码</param>
        /// <returns>如果是指定的错误码返回true,否则返回false</returns>
        protected bool IsIgnorableSocketError(int errorCode)
        {
            return errorCode == 10058 || errorCode == 10053 || errorCode == 10054 || errorCode == 995;
        }

        /// <summary>
        /// Socket事件参数完成时的处理方法
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">Socket异步事件参数</param>
        protected abstract void SocketEventArgsCompleted(object sender, SocketAsyncEventArgs e);

        /// <summary>
        /// 连接到远程终结点
        /// </summary>
        /// <param name="remoteEndPoint">远程终结点</param>
        /// <exception cref="ArgumentNullException">remoteEndPoint为null时抛出</exception>
        /// <exception cref="Exception">当正在连接或已经连接时抛出</exception>
        public override void Connect(EndPoint remoteEndPoint)
        {
            if (remoteEndPoint == null)
            {
                throw new ArgumentNullException("remoteEndPoint");
            }

            DnsEndPoint dnsEndPoint = remoteEndPoint as DnsEndPoint;
            if (dnsEndPoint != null)
            {
                string host = dnsEndPoint.Host;
                if (!string.IsNullOrEmpty(host))
                {
                    this.HostName = host;
                }
            }

            if (this.m_InConnecting)
            {
                throw new Exception("The socket is connecting, cannot connect again!");
            }

            if (base.Client != null)
            {
                throw new Exception("The socket is connected, you needn't connect again!");
            }

            if (base.Proxy != null)
            {
                base.Proxy.Completed += this.Proxy_Completed;
                base.Proxy.Connect(remoteEndPoint);
                this.m_InConnecting = true;
                return;
            }

            this.m_InConnecting = true;
            remoteEndPoint.ConnectAsync(this.LocalEndPoint, new ConnectedCallback(this.ProcessConnect), null);
        }

        /// <summary>
        /// 代理完成事件处理
        /// </summary>
        private void Proxy_Completed(object sender, ProxyEventArgs e)
        {
            base.Proxy.Completed -= this.Proxy_Completed;
            if (e.Connected)
            {
                SocketAsyncEventArgs socketAsyncEventArgs = null;
                if (e.TargetHostName != null)
                {
                    socketAsyncEventArgs = new SocketAsyncEventArgs();
                    socketAsyncEventArgs.RemoteEndPoint = new DnsEndPoint(e.TargetHostName, 0);
                }

                this.ProcessConnect(e.Socket, null, socketAsyncEventArgs, null);
                return;
            }

            this.OnError(new Exception("proxy error", e.Exception));
            this.m_InConnecting = false;
        }

        /// <summary>
        /// 处理连接结果
        /// </summary>
        protected void ProcessConnect(Socket socket, object state, SocketAsyncEventArgs e, Exception exception)
        {
            if (exception != null)
            {
                this.m_InConnecting = false;
                this.OnError(exception);
                if (e != null)
                {
                    e.Dispose();
                }

                return;
            }

            if (e != null && e.SocketError != SocketError.Success)
            {
                this.m_InConnecting = false;
                this.OnError(new SocketException((int)e.SocketError));
                e.Dispose();
                return;
            }

            if (socket == null)
            {
                this.m_InConnecting = false;
                this.OnError(new SocketException(10053));
                return;
            }

            if (!socket.Connected)
            {
                this.m_InConnecting = false;
                SocketError errorCode = SocketError.HostUnreachable;
                this.OnError(new SocketException((int)errorCode));
                return;
            }

            if (e == null)
            {
                e = new SocketAsyncEventArgs();
            }

            e.Completed += this.SocketEventArgsCompleted;
            base.Client = socket;
            this.m_InConnecting = false;
            try
            {
                this.LocalEndPoint = socket.LocalEndPoint;
            }
            catch
            {
            }

            EndPoint endPoint = (e.RemoteEndPoint != null) ? e.RemoteEndPoint : socket.RemoteEndPoint;
            if (string.IsNullOrEmpty(this.HostName))
            {
                this.HostName = this.GetHostOfEndPoint(endPoint);
            }
            else
            {
                DnsEndPoint dnsEndPoint = endPoint as DnsEndPoint;
                if (dnsEndPoint != null)
                {
                    string host = dnsEndPoint.Host;
                    if (!string.IsNullOrEmpty(host) && !this.HostName.Equals(host, StringComparison.OrdinalIgnoreCase))
                    {
                        this.HostName = host;
                    }
                }
            }

            this.OnGetSocket(e);
        }

        /// <summary>
        /// 获取终结点的主机名
        /// </summary>
        private string GetHostOfEndPoint(EndPoint endPoint)
        {
            DnsEndPoint dnsEndPoint = endPoint as DnsEndPoint;
            if (dnsEndPoint != null)
            {
                return dnsEndPoint.Host;
            }

            IPEndPoint ipendPoint = endPoint as IPEndPoint;
            if (ipendPoint != null && ipendPoint.Address != null)
            {
                return ipendPoint.Address.ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取Socket时的处理方法
        /// </summary>
        protected abstract void OnGetSocket(SocketAsyncEventArgs e);

        /// <summary>
        /// 确保Socket已关闭
        /// </summary>
        protected bool EnsureSocketClosed()
        {
            return this.EnsureSocketClosed(null);
        }

        /// <summary>
        /// 确保指定的Socket已关闭
        /// </summary>
        protected bool EnsureSocketClosed(Socket prevClient)
        {
            Socket socket = base.Client;
            if (socket == null)
            {
                return false;
            }

            bool result = true;
            if (prevClient != null && prevClient != socket)
            {
                socket = prevClient;
                result = false;
            }
            else
            {
                base.Client = null;
                this.m_IsSending = 0;
            }

            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }
            finally
            {
                try
                {
                    socket.Dispose();
                }
                catch
                {
                }
            }

            return result;
        }

        /// <summary>
        /// 检测是否已连接
        /// </summary>
        private bool DetectConnected()
        {
            if (base.Client != null)
            {
                return true;
            }

            this.OnError(new SocketException(10057));
            return false;
        }

        /// <summary>
        /// 获取发送队列
        /// </summary>
        private IBatchQueue<ArraySegment<byte>> GetSendingQueue()
        {
            if (this.m_SendingQueue != null)
            {
                return this.m_SendingQueue;
            }

            IBatchQueue<ArraySegment<byte>> sendingQueue;
            lock (this)
            {
                if (this.m_SendingQueue != null)
                {
                    sendingQueue = this.m_SendingQueue;
                }
                else
                {
                    this.m_SendingQueue = new ConcurrentBatchQueue<ArraySegment<byte>>(Math.Max(base.SendingQueueSize, 1024), (ArraySegment<byte> t) => t.Array == null || t.Count == 0);
                    sendingQueue = this.m_SendingQueue;
                }
            }

            return sendingQueue;
        }

        /// <summary>
        /// 获取发送项列表
        /// </summary>
        private PosList<ArraySegment<byte>> GetSendingItems()
        {
            if (this.m_SendingItems == null)
            {
                this.m_SendingItems = new PosList<ArraySegment<byte>>();
            }

            return this.m_SendingItems;
        }

        /// <summary>
        /// 获取是否正在发送数据
        /// </summary>
        protected bool IsSending
        {
            get { return this.m_IsSending == 1; }
        }

        /// <summary>
        /// 尝试发送数据
        /// </summary>
        /// <param name="segment">要发送的数据段</param>
        /// <returns>如果成功加入发送队列返回true,否则返回false</returns>
        public override bool TrySend(ArraySegment<byte> segment)
        {
            if (segment.Array == null || segment.Count == 0)
            {
                throw new Exception("The data to be sent cannot be empty.");
            }

            if (!this.DetectConnected())
            {
                return true;
            }

            bool result = this.GetSendingQueue().Enqueue(segment);
            if (Interlocked.CompareExchange(ref this.m_IsSending, 1, 0) != 0)
            {
                return result;
            }

            this.DequeueSend();
            return result;
        }

        /// <summary>
        /// 尝试发送多个数据段
        /// </summary>
        /// <param name="segments">要发送的数据段列表</param>
        /// <returns>如果成功加入发送队列返回true,否则返回false</returns>
        public override bool TrySend(IList<ArraySegment<byte>> segments)
        {
            if (segments == null || segments.Count == 0)
            {
                throw new ArgumentNullException("segments");
            }

            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].Count == 0)
                {
                    throw new Exception("The data piece to be sent cannot be empty.");
                }
            }

            if (!this.DetectConnected())
            {
                return true;
            }

            bool result = this.GetSendingQueue().Enqueue(segments);
            if (Interlocked.CompareExchange(ref this.m_IsSending, 1, 0) != 0)
            {
                return result;
            }

            this.DequeueSend();
            return result;
        }

        /// <summary>
        /// 从发送队列中取出数据并发送
        /// </summary>
        private void DequeueSend()
        {
            PosList<ArraySegment<byte>> sendingItems = this.GetSendingItems();
            if (!this.m_SendingQueue.TryDequeue(sendingItems))
            {
                this.m_IsSending = 0;
                return;
            }

            this.SendInternal(sendingItems);
        }

        /// <summary>
        /// 内部发送数据的方法
        /// </summary>
        /// <param name="items">要发送的数据项列表</param>
        protected abstract void SendInternal(PosList<ArraySegment<byte>> items);

        /// <summary>
        /// 发送完成时的处理方法
        /// </summary>
        protected void OnSendingCompleted()
        {
            PosList<ArraySegment<byte>> sendingItems = this.GetSendingItems();
            sendingItems.Clear();
            sendingItems.Position = 0;
            if (!this.m_SendingQueue.TryDequeue(sendingItems))
            {
                this.m_IsSending = 0;
                return;
            }

            this.SendInternal(sendingItems);
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public override void Close()
        {
            if (this.EnsureSocketClosed())
            {
                this.OnClosed();
            }
        }

        private bool m_InConnecting;

        /// <summary>
        /// 获取是否正在连接中
        /// </summary>
        public bool IsInConnecting
        {
            get { return this.m_InConnecting; }
        }

        private IBatchQueue<ArraySegment<byte>> m_SendingQueue;

        private PosList<ArraySegment<byte>> m_SendingItems;

        private int m_IsSending;
    }
}