using System.Net;
using System.Net.Sockets;

namespace GameFrameX.SuperSocket.ClientEngine
{
    /// <summary>
    /// 客户端会话的抽象基类
    /// </summary>
    public abstract class ClientSession : IClientSession, IBufferSetter
    {
        /// <summary>
        /// 获取或设置客户端Socket对象
        /// </summary>
        protected Socket Client { get; set; }

        /// <summary>
        /// 获取会话唯一标识符
        /// </summary>
        public string SessionID { get; private set; }

        /// <summary>
        /// 异步发送字节数组数据
        /// </summary>
        /// <param name="data">要发送的数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示异步操作的任务</returns>
        public ValueTask SendAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            return SendAsync(new ArraySegment<byte>(data), cancellationToken);
        }

        /// <summary>
        /// 异步发送只读内存数据
        /// </summary>
        /// <param name="data">要发送的数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示异步操作的任务</returns>
        public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            Send(data.ToArray(), 0, data.Length);
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// 获取底层Socket对象
        /// </summary>
        Socket IClientSession.Socket
        {
            get { return this.Client; }
        }

        /// <summary>
        /// 获取或设置本地终结点
        /// </summary>
        public virtual EndPoint LocalEndPoint { get; set; }

        /// <summary>
        /// 获取会话是否已连接
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// 获取或设置是否禁用Nagle算法
        /// </summary>
        public bool NoDelay { get; set; }

        /// <summary>
        /// 初始化客户端会话实例
        /// </summary>
        public ClientSession()
        {
            SessionID = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 获取或设置发送队列大小
        /// </summary>
        public int SendingQueueSize { get; set; }

        /// <summary>
        /// 连接到远程终结点
        /// </summary>
        /// <param name="remoteEndPoint">远程终结点</param>
        public abstract void Connect(EndPoint remoteEndPoint);

        /// <summary>
        /// 尝试发送单个数据段
        /// </summary>
        /// <param name="segment">要发送的数据段</param>
        /// <returns>发送是否成功</returns>
        public abstract bool TrySend(ArraySegment<byte> segment);

        /// <summary>
        /// 尝试发送多个数据段
        /// </summary>
        /// <param name="segments">要发送的数据段列表</param>
        /// <returns>发送是否成功</returns>
        public abstract bool TrySend(IList<ArraySegment<byte>> segments);

        /// <summary>
        /// 发送字节数组数据
        /// </summary>
        /// <param name="data">要发送的数据</param>
        /// <param name="offset">起始偏移量</param>
        /// <param name="length">数据长度</param>
        public void Send(byte[] data, int offset, int length)
        {
            this.Send(new ArraySegment<byte>(data, offset, length));
        }

        /// <summary>
        /// 发送单个数据段
        /// </summary>
        /// <param name="segment">要发送的数据段</param>
        public void Send(ArraySegment<byte> segment)
        {
            if (this.TrySend(segment))
            {
                return;
            }

            SpinWait spinWait = default(SpinWait);
            do
            {
                spinWait.SpinOnce();
            } while (!this.TrySend(segment));
        }

        /// <summary>
        /// 发送多个数据段
        /// </summary>
        /// <param name="segments">要发送的数据段列表</param>
        public void Send(IList<ArraySegment<byte>> segments)
        {
            if (this.TrySend(segments))
            {
                return;
            }

            SpinWait spinWait = default(SpinWait);
            do
            {
                spinWait.SpinOnce();
            } while (!this.TrySend(segments));
        }

        /// <summary>
        /// 关闭会话连接
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// 会话关闭事件
        /// </summary>
        public event EventHandler Closed
        {
            add { this.m_Closed = (EventHandler)Delegate.Combine(this.m_Closed, value); }
            remove { this.m_Closed = (EventHandler)Delegate.Remove(this.m_Closed, value); }
        }

        /// <summary>
        /// 处理会话关闭事件
        /// </summary>
        protected virtual void OnClosed()
        {
            this.IsConnected = false;
            this.LocalEndPoint = null;
            EventHandler closed = this.m_Closed;
            if (closed != null)
            {
                closed(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 错误事件
        /// </summary>
        public event EventHandler<ErrorEventArgs> Error
        {
            add { this.m_Error = (EventHandler<ErrorEventArgs>)Delegate.Combine(this.m_Error, value); }
            remove { this.m_Error = (EventHandler<ErrorEventArgs>)Delegate.Remove(this.m_Error, value); }
        }

        /// <summary>
        /// 处理错误事件
        /// </summary>
        /// <param name="e">异常对象</param>
        protected virtual void OnError(Exception e)
        {
            EventHandler<ErrorEventArgs> error = this.m_Error;
            if (error == null)
            {
                return;
            }

            error(this, new ErrorEventArgs(e));
        }

        /// <summary>
        /// 连接成功事件
        /// </summary>
        public event EventHandler Connected
        {
            add { this.m_Connected = (EventHandler)Delegate.Combine(this.m_Connected, value); }
            remove { this.m_Connected = (EventHandler)Delegate.Remove(this.m_Connected, value); }
        }

        /// <summary>
        /// 处理连接成功事件
        /// </summary>
        protected virtual void OnConnected()
        {
            Socket client = this.Client;
            if (client != null)
            {
                try
                {
                    if (client.NoDelay != this.NoDelay)
                    {
                        client.NoDelay = this.NoDelay;
                    }
                }
                catch
                {
                }
            }

            this.IsConnected = true;
            EventHandler connected = this.m_Connected;
            if (connected == null)
            {
                return;
            }

            connected(this, EventArgs.Empty);
        }

        /// <summary>
        /// 数据接收事件
        /// </summary>
        public event EventHandler<DataEventArgs> DataReceived
        {
            add { this.m_DataReceived = (EventHandler<DataEventArgs>)Delegate.Combine(this.m_DataReceived, value); }
            remove { this.m_DataReceived = (EventHandler<DataEventArgs>)Delegate.Remove(this.m_DataReceived, value); }
        }

        /// <summary>
        /// 处理数据接收事件
        /// </summary>
        /// <param name="data">接收到的数据</param>
        /// <param name="offset">数据偏移量</param>
        /// <param name="length">数据长度</param>
        protected virtual void OnDataReceived(byte[] data, int offset, int length)
        {
            EventHandler<DataEventArgs> dataReceived = this.m_DataReceived;
            if (dataReceived == null)
            {
                return;
            }

            this.m_DataArgs.Data = data;
            this.m_DataArgs.Offset = offset;
            this.m_DataArgs.Length = length;
            dataReceived(this, this.m_DataArgs);
        }

        /// <summary>
        /// 获取或设置接收缓冲区大小
        /// </summary>
        public virtual int ReceiveBufferSize { get; set; }

        /// <summary>
        /// 获取或设置代理连接器
        /// </summary>
        public IProxyConnector Proxy { get; set; }

        /// <summary>
        /// 获取或设置缓冲区
        /// </summary>
        protected ArraySegment<byte> Buffer { get; set; }

        /// <summary>
        /// 设置缓冲区
        /// </summary>
        /// <param name="bufferSegment">缓冲区段</param>
        void IBufferSetter.SetBuffer(ArraySegment<byte> bufferSegment)
        {
            this.SetBuffer(bufferSegment);
        }

        /// <summary>
        /// 设置缓冲区的虚拟方法
        /// </summary>
        /// <param name="bufferSegment">缓冲区段</param>
        protected virtual void SetBuffer(ArraySegment<byte> bufferSegment)
        {
            this.Buffer = bufferSegment;
        }

        /// <summary>
        /// 默认接收缓冲区大小
        /// </summary>
        public const int DefaultReceiveBufferSize = 4096;

        private EventHandler m_Closed;

        private EventHandler<ErrorEventArgs> m_Error;

        private EventHandler m_Connected;

        private EventHandler<DataEventArgs> m_DataReceived;

        private DataEventArgs m_DataArgs = new DataEventArgs();
    }
}