using System.IO.Pipelines;
using Microsoft.Extensions.Logging;

namespace GameFrameX.SuperSocket.Connection
{
    /// <summary>
    /// 连接配置选项类
    /// </summary>
    public class ConnectionOptions
    {
        /// <summary>
        /// 最大数据包长度，默认为1M (1024 * 1024)
        /// </summary>
        public int MaxPackageLength { get; set; } = 1024 * 1024;

        /// <summary>
        /// 接收缓冲区大小，默认为4K (1024 * 4)
        /// </summary>
        public int ReceiveBufferSize { get; set; } = 1024 * 4;

        /// <summary>
        /// 发送缓冲区大小，默认为4K (1024 * 4)
        /// </summary>
        public int SendBufferSize { get; set; } = 1024 * 4;

        /// <summary>
        /// 是否按需读取，仅在流被消费时触发读取操作
        /// </summary>
        public bool ReadAsDemand { get; set; }

        /// <summary>
        /// 接收超时时间，以毫秒为单位
        /// </summary>
        public int ReceiveTimeout { get; set; }

        /// <summary>
        /// 发送超时时间，以毫秒为单位
        /// </summary>
        public int SendTimeout { get; set; }

        /// <summary>
        /// 日志记录器实例
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// 输入管道
        /// </summary>
        public Pipe Input { get; set; }

        /// <summary>
        /// 输出管道
        /// </summary>
        public Pipe Output { get; set; }

        /// <summary>
        /// 自定义键值对集合
        /// </summary>
        public Dictionary<string, string> Values { get; set; }
    }
}