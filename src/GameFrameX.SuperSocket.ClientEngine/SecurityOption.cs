using System;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace GameFrameX.SuperSocket.ClientEngine
{
    /// <summary>
    /// 安全选项类，用于配置SSL/TLS连接的安全参数
    /// </summary>
    public class SecurityOption
    {
        /// <summary>
        /// 获取或设置启用的SSL协议版本
        /// </summary>
        public SslProtocols EnabledSslProtocols { get; set; }

        /// <summary>
        /// 获取或设置X509证书集合
        /// </summary>
        public X509CertificateCollection Certificates { get; set; }

        /// <summary>
        /// 获取或设置是否允许不受信任的证书
        /// </summary>
        public bool AllowUnstrustedCertificate { get; set; }

        /// <summary>
        /// 获取或设置是否允许证书名称不匹配
        /// </summary>
        public bool AllowNameMismatchCertificate { get; set; }

        /// <summary>
        /// 获取或设置是否允许证书链错误
        /// </summary>
        public bool AllowCertificateChainErrors { get; set; }

        /// <summary>
        /// 获取或设置网络凭据
        /// </summary>
        public NetworkCredential Credential { get; set; }

        /// <summary>
        /// 使用默认协议初始化安全选项的构造函数
        /// </summary>
        public SecurityOption() : this(SecurityOption.GetDefaultProtocol(), new X509CertificateCollection())
        {
        }

        /// <summary>
        /// 使用指定的SSL协议初始化安全选项的构造函数
        /// </summary>
        /// <param name="enabledSslProtocols">启用的SSL协议版本</param>
        public SecurityOption(SslProtocols enabledSslProtocols) : this(enabledSslProtocols, new X509CertificateCollection())
        {
        }

        /// <summary>
        /// 使用指定的SSL协议和证书初始化安全选项的构造函数
        /// </summary>
        /// <param name="enabledSslProtocols">启用的SSL协议版本</param>
        /// <param name="certificate">X509证书</param>
        public SecurityOption(SslProtocols enabledSslProtocols, X509Certificate certificate) : this(enabledSslProtocols, new X509CertificateCollection(new X509Certificate[]
        {
            certificate
        }))
        {
        }

        /// <summary>
        /// 使用指定的SSL协议和证书集合初始化安全选项的构造函数
        /// </summary>
        /// <param name="enabledSslProtocols">启用的SSL协议版本</param>
        /// <param name="certificates">X509证书集合</param>
        public SecurityOption(SslProtocols enabledSslProtocols, X509CertificateCollection certificates)
        {
            this.EnabledSslProtocols = enabledSslProtocols;
            this.Certificates = certificates;
        }

        /// <summary>
        /// 使用网络凭据初始化安全选项的构造函数
        /// </summary>
        /// <param name="credential">网络凭据</param>
        public SecurityOption(NetworkCredential credential)
        {
            this.Credential = credential;
        }

        /// <summary>
        /// 获取默认的SSL协议版本
        /// </summary>
        /// <returns>返回TLS1.2和TLS1.3的组合</returns>
        private static SslProtocols GetDefaultProtocol()
        {
            return SslProtocols.Tls12 | SslProtocols.Tls13;
        }
    }
}