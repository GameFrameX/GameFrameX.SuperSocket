namespace GameFrameX.SuperSocket.Server.Abstractions.Session
{
    /// <summary>
    /// 会话状态枚举
    /// </summary>
    public enum SessionState
    {
        /// <summary>
        /// 初始状态，未初始化
        /// </summary>
        None = 0,

        /// <summary>
        /// 已初始化状态
        /// </summary>
        Initialized = 1,

        /// <summary>
        /// 已连接状态
        /// </summary>
        Connected = 2,

        /// <summary>
        /// 已关闭状态
        /// </summary>
        Closed = 3
    }
}