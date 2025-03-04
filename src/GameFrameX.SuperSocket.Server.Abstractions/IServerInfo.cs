using System;

namespace GameFrameX.SuperSocket.Server.Abstractions
{
    /// <summary>
    /// 服务器信息接口
    /// </summary>
    public interface IServerInfo
    {
        /// <summary>
        /// 获取服务器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 获取服务器配置选项
        /// </summary>
        ServerOptions Options { get; }

        /// <summary>
        /// 获取或设置数据上下文
        /// </summary>
        object DataContext { get; set; }

        /// <summary>
        /// 获取当前会话数量
        /// </summary>
        int SessionCount { get; }

        /// <summary>
        /// 获取服务提供者
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// 获取服务器当前状态
        /// </summary>
        ServerState State { get; }
    }
}