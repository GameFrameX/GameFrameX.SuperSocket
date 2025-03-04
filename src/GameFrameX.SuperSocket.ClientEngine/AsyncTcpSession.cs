using System;
using System.Net.Sockets;

namespace GameFrameX.SuperSocket.ClientEngine
{
    /// <summary>
    /// 异步TCP会话类，用于处理TCP客户端的异步通信
    /// </summary>
    public class AsyncTcpSession : TcpClientSession
    {
        /// <summary>
        /// 处理Socket异步事件完成的回调方法
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">Socket异步事件参数</param>
        protected override void SocketEventArgsCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Connect)
            {
                base.ProcessConnect(sender as Socket, null, e, null);
                return;
            }

            this.ProcessReceive(e);
        }

        /// <summary>
        /// 设置缓冲区
        /// </summary>
        /// <param name="bufferSegment">缓冲区数据段</param>
        protected override void SetBuffer(ArraySegment<byte> bufferSegment)
        {
            base.SetBuffer(bufferSegment);
            if (this.m_SocketEventArgs != null)
            {
                this.m_SocketEventArgs.SetBuffer(bufferSegment.Array, bufferSegment.Offset, bufferSegment.Count);
            }
        }

        /// <summary>
        /// 获取Socket时的处理方法
        /// </summary>
        /// <param name="e">Socket异步事件参数</param>
        protected override void OnGetSocket(SocketAsyncEventArgs e)
        {
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

            e.SetBuffer(base.Buffer.Array, base.Buffer.Offset, base.Buffer.Count);
            this.m_SocketEventArgs = e;
            this.OnConnected();
            this.StartReceive();
        }

        /// <summary>
        /// 开始接收数据
        /// </summary>
        private void BeginReceive()
        {
            if (!base.Client.ReceiveAsync(this.m_SocketEventArgs))
            {
                this.ProcessReceive(this.m_SocketEventArgs);
            }
        }

        /// <summary>
        /// 处理接收到的数据
        /// </summary>
        /// <param name="e">Socket异步事件参数</param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                if (base.EnsureSocketClosed())
                {
                    this.OnClosed();
                }

                if (!base.IsIgnorableSocketError((int)e.SocketError))
                {
                    this.OnError(new SocketException((int)e.SocketError));
                }

                return;
            }

            if (e.BytesTransferred == 0)
            {
                if (base.EnsureSocketClosed())
                {
                    this.OnClosed();
                }

                return;
            }

            this.OnDataReceived(e.Buffer, e.Offset, e.BytesTransferred);
            this.StartReceive();
        }

        /// <summary>
        /// 启动数据接收
        /// </summary>
        private void StartReceive()
        {
            Socket client = base.Client;
            if (client == null)
            {
                return;
            }

            bool flag;
            try
            {
                flag = client.ReceiveAsync(this.m_SocketEventArgs);
            }
            catch (SocketException ex)
            {
                int socketErrorCode = (int)ex.SocketErrorCode;
                if (!base.IsIgnorableSocketError(socketErrorCode))
                {
                    this.OnError(ex);
                }

                if (base.EnsureSocketClosed(client))
                {
                    this.OnClosed();
                }

                return;
            }
            catch (Exception e)
            {
                if (!this.IsIgnorableException(e))
                {
                    this.OnError(e);
                }

                if (base.EnsureSocketClosed(client))
                {
                    this.OnClosed();
                }

                return;
            }

            if (!flag)
            {
                this.ProcessReceive(this.m_SocketEventArgs);
            }
        }

        /// <summary>
        /// 内部发送数据方法
        /// </summary>
        /// <param name="items">要发送的数据段列表</param>
        protected override void SendInternal(PosList<ArraySegment<byte>> items)
        {
            if (this.m_SocketEventArgsSend == null)
            {
                this.m_SocketEventArgsSend = new SocketAsyncEventArgs();
                this.m_SocketEventArgsSend.Completed += this.Sending_Completed;
            }

            bool flag;
            try
            {
                if (items.Count > 1)
                {
                    if (this.m_SocketEventArgsSend.Buffer != null)
                    {
                        this.m_SocketEventArgsSend.SetBuffer(null, 0, 0);
                    }

                    this.m_SocketEventArgsSend.BufferList = items;
                }
                else
                {
                    ArraySegment<byte> arraySegment = items[0];
                    try
                    {
                        if (this.m_SocketEventArgsSend.BufferList != null)
                        {
                            this.m_SocketEventArgsSend.BufferList = null;
                        }
                    }
                    catch
                    {
                    }

                    this.m_SocketEventArgsSend.SetBuffer(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
                }

                flag = base.Client.SendAsync(this.m_SocketEventArgsSend);
            }
            catch (SocketException ex)
            {
                int socketErrorCode = (int)ex.SocketErrorCode;
                if (base.EnsureSocketClosed() && !base.IsIgnorableSocketError(socketErrorCode))
                {
                    this.OnError(ex);
                }

                return;
            }
            catch (Exception e)
            {
                if (base.EnsureSocketClosed() && this.IsIgnorableException(e))
                {
                    this.OnError(e);
                }

                return;
            }

            if (!flag)
            {
                this.Sending_Completed(base.Client, this.m_SocketEventArgsSend);
            }
        }

        /// <summary>
        /// 发送完成的回调处理方法
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">Socket异步事件参数</param>
        private void Sending_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success || e.BytesTransferred == 0)
            {
                if (base.EnsureSocketClosed())
                {
                    this.OnClosed();
                }

                if (e.SocketError != SocketError.Success && !base.IsIgnorableSocketError((int)e.SocketError))
                {
                    this.OnError(new SocketException((int)e.SocketError));
                }

                return;
            }

            base.OnSendingCompleted();
        }

        /// <summary>
        /// 连接关闭时的处理方法
        /// </summary>
        protected override void OnClosed()
        {
            if (this.m_SocketEventArgsSend != null)
            {
                this.m_SocketEventArgsSend.Dispose();
                this.m_SocketEventArgsSend = null;
            }

            if (this.m_SocketEventArgs != null)
            {
                this.m_SocketEventArgs.Dispose();
                this.m_SocketEventArgs = null;
            }

            base.OnClosed();
        }

        /// <summary>
        /// Socket异步事件参数，用于接收数据
        /// </summary>
        private SocketAsyncEventArgs m_SocketEventArgs;

        /// <summary>
        /// Socket异步事件参数，用于发送数据
        /// </summary>
        private SocketAsyncEventArgs m_SocketEventArgsSend;
    }
}