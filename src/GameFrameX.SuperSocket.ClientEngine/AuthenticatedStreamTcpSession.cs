using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;

namespace GameFrameX.SuperSocket.ClientEngine
{
    /// <summary>
    /// 经过身份验证的TCP会话的抽象基类
    /// </summary>
    public abstract class AuthenticatedStreamTcpSession : TcpClientSession
    {
        /// <summary>
        /// 初始化 AuthenticatedStreamTcpSession 的新实例
        /// </summary>
        public AuthenticatedStreamTcpSession()
        {
        }

        /// <summary>
        /// 获取或设置安全选项
        /// </summary>
        public SecurityOption Security { get; set; }

        /// <summary>
        /// 处理Socket事件完成的回调方法
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">Socket异步事件参数</param>
        protected override void SocketEventArgsCompleted(object sender, SocketAsyncEventArgs e)
        {
            base.ProcessConnect(sender as Socket, null, e, null);
        }

        /// <summary>
        /// 启动经过身份验证的流
        /// </summary>
        /// <param name="client">客户端Socket</param>
        protected abstract void StartAuthenticatedStream(Socket client);

        /// <summary>
        /// 获取Socket时的处理方法
        /// </summary>
        /// <param name="e">Socket异步事件参数</param>
        protected override void OnGetSocket(SocketAsyncEventArgs e)
        {
            try
            {
                this.StartAuthenticatedStream(base.Client);
            }
            catch (Exception e2)
            {
                if (!this.IsIgnorableException(e2))
                {
                    this.OnError(e2);
                }
            }
        }

        /// <summary>
        /// 当验证流连接成功时的处理方法
        /// </summary>
        /// <param name="stream">已验证的流</param>
        protected void OnAuthenticatedStreamConnected(AuthenticatedStream stream)
        {
            this.m_Stream = stream;
            this.OnConnected();
            if (base.Buffer.Array == null)
            {
                int num = this.ReceiveBufferSize;
                if (num <= 0)
                {
                    num = 4096;
                }

                this.ReceiveBufferSize = num;
                base.Buffer = new ArraySegment<byte>(new byte[num]);
            }

            this.BeginRead();
        }

        /// <summary>
        /// 开始读取数据
        /// </summary>
        private void BeginRead()
        {
            this.ReadAsync();
        }

        /// <summary>
        /// 异步读取数据
        /// </summary>
        private async void ReadAsync()
        {
            while (base.IsConnected)
            {
                if (base.Client != null && this.m_Stream != null)
                {
                    ArraySegment<byte> buffer = base.Buffer;
                    int length = 0;
                    try
                    {
                        int num = await this.m_Stream.ReadAsync(buffer.Array, buffer.Offset, buffer.Count, CancellationToken.None);
                        length = num;
                    }
                    catch (Exception e)
                    {
                        if (!this.IsIgnorableException(e))
                        {
                            this.OnError(e);
                        }

                        if (base.EnsureSocketClosed(base.Client))
                        {
                            this.OnClosed();
                        }

                        break;
                    }

                    if (length != 0)
                    {
                        this.OnDataReceived(buffer.Array, buffer.Offset, length);
                        buffer = default(ArraySegment<byte>);
                        continue;
                    }

                    if (base.EnsureSocketClosed(base.Client))
                    {
                        this.OnClosed();
                    }
                }

                return;
            }
        }

        /// <summary>
        /// 判断异常是否可以忽略
        /// </summary>
        /// <param name="e">异常对象</param>
        /// <returns>如果异常可以忽略返回true，否则返回false</returns>
        protected override bool IsIgnorableException(Exception e)
        {
            if (base.IsIgnorableException(e))
            {
                return true;
            }

            if (e is IOException)
            {
                if (e.InnerException is ObjectDisposedException)
                {
                    return true;
                }

                if (e.InnerException is IOException && e.InnerException.InnerException is ObjectDisposedException)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 内部发送数据的方法
        /// </summary>
        /// <param name="items">要发送的数据项列表</param>
        protected override void SendInternal(PosList<ArraySegment<byte>> items)
        {
            this.SendInternalAsync(items);
        }

        /// <summary>
        /// 异步发送数据的内部方法
        /// </summary>
        /// <param name="items">要发送的数据项列表</param>
        private async void SendInternalAsync(PosList<ArraySegment<byte>> items)
        {
            try
            {
                for (int i = items.Position; i < items.Count; i++)
                {
                    ArraySegment<byte> arraySegment = items[i];
                    await this.m_Stream.WriteAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count, CancellationToken.None);
                }

                this.m_Stream.Flush();
            }
            catch (Exception e)
            {
                if (!this.IsIgnorableException(e))
                {
                    this.OnError(e);
                }

                if (base.EnsureSocketClosed(base.Client))
                {
                    this.OnClosed();
                }

                return;
            }

            base.OnSendingCompleted();
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public override void Close()
        {
            AuthenticatedStream stream = this.m_Stream;
            if (stream != null)
            {
                stream.Dispose();
                this.m_Stream = null;
            }

            base.Close();
        }

        /// <summary>
        /// 已验证的流对象
        /// </summary>
        private AuthenticatedStream m_Stream;

        /// <summary>
        /// 流异步状态类
        /// </summary>
        private class StreamAsyncState
        {
            /// <summary>
            /// 获取或设置已验证的流
            /// </summary>
            public AuthenticatedStream Stream { get; set; }

            /// <summary>
            /// 获取或设置客户端Socket
            /// </summary>
            public Socket Client { get; set; }

            /// <summary>
            /// 获取或设置发送项列表
            /// </summary>
            public PosList<ArraySegment<byte>> SendingItems { get; set; }
        }
    }
}