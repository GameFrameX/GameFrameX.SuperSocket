using System.Collections.Generic;
using System.Text;
using GameFrameX.SuperSocket.Connection;

namespace GameFrameX.SuperSocket.Server.Abstractions
{
    /// <summary>
    /// 服务器配置选项类
    /// </summary>
    public class ServerOptions : ConnectionOptions
    {
        /// <summary>
        /// 获取或设置服务器名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置监听器配置列表
        /// </summary>
        public List<ListenOptions> Listeners { get; set; }

        /// <summary>
        /// 获取或设置默认的文本编码
        /// </summary>
        public Encoding DefaultTextEncoding { get; set; }

        /// <summary>
        /// 获取或设置清理空闲会话的时间间隔（单位：秒）
        /// </summary>
        public int ClearIdleSessionInterval { get; set; } = 120;

        /// <summary>
        /// 获取或设置会话空闲超时时间（单位：秒）
        /// </summary>
        public int IdleSessionTimeOut { get; set; } = 300;

        /// <summary>
        /// 获取或设置包处理超时时间（单位：秒）
        /// </summary>
        public int PackageHandlingTimeOut { get; set; } = 30;
    }
}