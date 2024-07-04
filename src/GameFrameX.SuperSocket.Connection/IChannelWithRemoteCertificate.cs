using System.Security.Cryptography.X509Certificates;

namespace GameFrameX.SuperSocket.Connection
{
    public interface IConnectionWithRemoteCertificate
    {
        X509Certificate RemoteCertificate { get; }
    }
}