using System;
using System.Net;
using System.Security.Authentication;
using GameFrameX.SuperSocket.Primitives;

namespace GameFrameX.SuperSocket.Server.Abstractions
{
    /// <summary>
    /// 监听选项配置类
    /// </summary>
    public class ListenOptions
    {
        /// <summary>
        /// 获取或设置IP地址
        /// </summary>
        public string Ip { get; set; }

        /// <summary>
        /// 获取或设置端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 获取或设置路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 获取或设置积压数量（挂起连接队列的最大长度）
        /// </summary>
        public int BackLog { get; set; }

        /// <summary>
        /// 获取或设置是否禁用Nagle算法
        /// </summary>
        public bool NoDelay { get; set; }

        /// <summary>
        /// 获取或设置SSL协议类型
        /// </summary>
        public SslProtocols Security { get; set; }

        /// <summary>
        /// 获取或设置证书选项
        /// </summary>
        public CertificateOptions CertificateOptions { get; set; }

        /// <summary>
        /// 获取或设置连接接受超时时间，默认为5秒
        /// </summary>
        public TimeSpan ConnectionAcceptTimeOut { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 获取或设置UDP是否使用独占地址，默认为true
        /// </summary>
        public bool UdpExclusiveAddressUse { get; set; } = true;

        /// <summary>
        /// 将当前监听选项转换为IPEndPoint对象
        /// </summary>
        /// <returns>返回对应的IPEndPoint实例</returns>
        public IPEndPoint ToEndPoint()
        {
            var ip = this.Ip;
            var port = this.Port;

            IPAddress ipAddress;

            if ("any".Equals(ip, StringComparison.OrdinalIgnoreCase))
            {
                ipAddress = IPAddress.Any;
            }
            else if ("IpV6Any".Equals(ip, StringComparison.OrdinalIgnoreCase))
            {
                ipAddress = IPAddress.IPv6Any;
            }
            else
            {
                ipAddress = IPAddress.Parse(ip);
            }

            return new IPEndPoint(ipAddress, port);
        }

        /// <summary>
        /// 重写ToString方法，返回监听选项的字符串表示
        /// </summary>
        /// <returns>返回包含监听选项详细信息的字符串</returns>
        public override string ToString()
        {
            return $"{nameof(Ip)}={Ip}, {nameof(Port)}={Port}, {nameof(Security)}={Security}, {nameof(Path)}={Path}, {nameof(BackLog)}={BackLog}, {nameof(NoDelay)}={NoDelay}";
        }
    }
}