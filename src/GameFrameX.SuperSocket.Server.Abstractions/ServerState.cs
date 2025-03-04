using System;
using System.Threading.Tasks;

namespace GameFrameX.SuperSocket.Server.Abstractions
{
    /// <summary>
    /// 服务器状态枚举
    /// </summary>
    public enum ServerState
    {
        /// <summary>
        /// 初始状态
        /// </summary>
        // Initial state.
        None = 0,

        /// <summary>
        /// 正在启动中
        /// </summary>
        // In starting.
        Starting = 1,

        /// <summary>
        /// 已启动
        /// </summary>
        // Started.
        Started = 2,

        /// <summary>
        /// 正在停止中
        /// </summary>
        // In stopping
        Stopping = 3,

        /// <summary>
        /// 已停止
        /// </summary>
        // Stopped.
        Stopped = 4,

        /// <summary>
        /// 启动失败
        /// </summary>
        // Failed to start.
        Failed = 5
    }
}