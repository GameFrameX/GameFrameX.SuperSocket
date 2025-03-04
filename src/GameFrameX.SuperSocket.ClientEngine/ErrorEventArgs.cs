using System;

namespace GameFrameX.SuperSocket.ClientEngine
{
    /// <summary>
    /// 错误事件参数类
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        /// <summary>
        /// 获取或设置异常对象
        /// </summary>
        /// <value>
        /// 包含错误详细信息的异常对象
        /// </value>
        public Exception Exception { get; private set; }

        /// <summary>
        /// 初始化 <see cref="ErrorEventArgs"/> 类的新实例
        /// </summary>
        /// <param name="exception">要封装的异常对象</param>
        public ErrorEventArgs(Exception exception)
        {
            this.Exception = exception;
        }
    }
}