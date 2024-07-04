using System.Net.Security;
using System.Net.Sockets;
using GameFrameX.SuperSocket.Server.Abstractions;
using GameFrameX.SuperSocket.Server.Abstractions.Connections;

namespace GameFrameX.SuperSocket.Server.Connection
{
    public class SslStreamInitializer : IConnectionStreamInitializer
    {
        private SslServerAuthenticationOptions _authOptions;

        public void Setup(ListenOptions listenOptions)
        {
            var authOptions = new SslServerAuthenticationOptions();

            authOptions.EnabledSslProtocols = listenOptions.Security;

            if (listenOptions.CertificateOptions.Certificate == null)
            {
                listenOptions.CertificateOptions.EnsureCertificate();
            }

            authOptions.ServerCertificate = listenOptions.CertificateOptions.Certificate;
            authOptions.ClientCertificateRequired = listenOptions.CertificateOptions.ClientCertificateRequired;

            if (listenOptions.CertificateOptions.RemoteCertificateValidationCallback != null)
                authOptions.RemoteCertificateValidationCallback = listenOptions.CertificateOptions.RemoteCertificateValidationCallback;

            _authOptions = authOptions;
        }

        public async Task<Stream> InitializeAsync(Socket socket, Stream stream, CancellationToken cancellationToken)
        {
            var sslStream = new SslStream(stream, false);
            await sslStream.AuthenticateAsServerAsync(_authOptions, cancellationToken);
            return sslStream;
        }
    }
}