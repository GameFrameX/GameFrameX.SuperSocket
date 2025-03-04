using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace GameFrameX.SuperSocket.ClientEngine
{
    /// <summary>
    /// SSL流TCP会话类，用于处理SSL/TLS加密通信
    /// </summary>
    public class SslStreamTcpSession : AuthenticatedStreamTcpSession
    {
        /// <summary>
        /// 启动认证流
        /// </summary>
        /// <param name="client">客户端Socket对象</param>
        /// <exception cref="Exception">当安全选项未配置时抛出异常</exception>
        protected override void StartAuthenticatedStream(Socket client)
        {
            if (base.Security == null)
            {
                throw new Exception("securityOption was not configured");
            }

            this.AuthenticateAsClientAsync(new SslStream(new NetworkStream(client), false, new RemoteCertificateValidationCallback(this.ValidateRemoteCertificate)), base.Security);
        }

        /// <summary>
        /// 异步进行客户端认证
        /// </summary>
        /// <param name="sslStream">SSL流对象</param>
        /// <param name="securityOption">安全选项配置</param>
        private async void AuthenticateAsClientAsync(SslStream sslStream, SecurityOption securityOption)
        {
            try
            {
                await sslStream.AuthenticateAsClientAsync(base.HostName, securityOption.Certificates, securityOption.EnabledSslProtocols, false);
            }
            catch (Exception e)
            {
                base.EnsureSocketClosed();
                this.OnError(e);
                return;
            }

            base.OnAuthenticatedStreamConnected(sslStream);
        }

        /// <summary>
        /// 验证远程证书
        /// </summary>
        /// <param name="sender">发送者对象</param>
        /// <param name="certificate">X509证书</param>
        /// <param name="chain">证书链</param>
        /// <param name="sslPolicyErrors">SSL策略错误</param>
        /// <returns>如果证书验证通过返回true，否则返回false</returns>
        private bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (base.Security.AllowNameMismatchCertificate)
            {
                sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNameMismatch;
            }

            if (base.Security.AllowCertificateChainErrors)
            {
                sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;
            }

            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (!base.Security.AllowUnstrustedCertificate)
            {
                this.OnError(new Exception(sslPolicyErrors.ToString()));
                return false;
            }

            if (sslPolicyErrors != SslPolicyErrors.None && sslPolicyErrors != SslPolicyErrors.RemoteCertificateChainErrors)
            {
                this.OnError(new Exception(sslPolicyErrors.ToString()));
                return false;
            }

            if (chain != null && chain.ChainStatus != null)
            {
                foreach (X509ChainStatus x509ChainStatus in chain.ChainStatus)
                {
                    if ((!(certificate.Subject == certificate.Issuer) || x509ChainStatus.Status != X509ChainStatusFlags.UntrustedRoot) && x509ChainStatus.Status != X509ChainStatusFlags.NoError)
                    {
                        this.OnError(new Exception(sslPolicyErrors.ToString()));
                        return false;
                    }
                }
            }

            return true;
        }
    }
}