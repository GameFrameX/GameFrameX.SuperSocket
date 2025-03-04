using System;
using System.Net.Sockets;

namespace GameFrameX.SuperSocket.ClientEngine
{
    /// <summary>
    /// 代理事件参数类
    /// </summary>
    public class ProxyEventArgs : EventArgs
    {
        /// <summary>
        /// 使用Socket初始化代理事件参数
        /// </summary>
        /// <param name="socket">Socket连接对象</param>
        public ProxyEventArgs(Socket socket) : this(true, socket, null, null)
        {
        }

        /// <summary>
        /// 使用Socket和目标主机名初始化代理事件参数
        /// </summary>
        /// <param name="socket">Socket连接对象</param>
        /// <param name="targetHostName">目标主机名</param>
        public ProxyEventArgs(Socket socket, string targetHostName) : this(true, socket, targetHostName, null)
        {
        }

        /// <summary>
        /// 使用异常信息初始化代理事件参数
        /// </summary>
        /// <param name="exception">异常信息</param>
        public ProxyEventArgs(Exception exception) : this(false, null, null, exception)
        {
        }

        /// <summary>
        /// 使用完整参数初始化代理事件参数
        /// </summary>
        /// <param name="connected">是否已连接</param>
        /// <param name="socket">Socket连接对象</param>
        /// <param name="targetHostName">目标主机名</param>
        /// <param name="exception">异常信息</param>
        public ProxyEventArgs(bool connected, Socket socket, string targetHostName, Exception exception)
        {
            this.Connected = connected;
            this.Socket = socket;
            this.TargetHostName = targetHostName;
            this.Exception = exception;
        }

        /// <summary>
        /// 获取是否已连接
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// 获取Socket连接对象
        /// </summary>
        public Socket Socket { get; private set; }

        /// <summary>
        /// 获取异常信息
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// 获取目标主机名
        /// </summary>
        public string TargetHostName { get; private set; }
    }
}