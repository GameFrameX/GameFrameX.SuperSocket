namespace GameFrameX.SuperSocket.Connection
{
    /// <summary>
    /// 连接关闭的原因枚举
    /// </summary>
    public enum CloseReason
    {
        /// <summary>
        /// 由于未知原因导致的Socket关闭
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 由于服务器关闭导致的连接断开
        /// </summary>
        ServerShutdown = 1,

        /// <summary>
        /// 远程端点主动发起的关闭行为
        /// </summary>
        RemoteClosing = 2,

        /// <summary>
        /// 本地端点主动发起的关闭行为
        /// </summary>
        LocalClosing = 3,

        /// <summary>
        /// 应用程序错误导致的关闭
        /// </summary>
        ApplicationError = 4,

        /// <summary>
        /// 由于Socket错误导致的关闭
        /// </summary>
        SocketError = 5,

        /// <summary>
        /// 由于服务器超时导致的Socket关闭
        /// </summary>
        TimeOut = 6,

        /// <summary>
        /// 由于协议错误导致的关闭
        /// </summary>
        ProtocolError = 7,

        /// <summary>
        /// SuperSocket内部错误导致的关闭
        /// </summary>
        InternalError = 8,
    }
}