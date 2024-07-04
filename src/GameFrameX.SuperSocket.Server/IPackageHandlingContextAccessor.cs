using GameFrameX.SuperSocket.Server.Abstractions.Session;

namespace GameFrameX.SuperSocket.Server
{
    public interface IPackageHandlingContextAccessor<TPackageInfo>
    {
        PackageHandlingContext<IAppSession, TPackageInfo> PackageHandlingContext { get; set; }
    }
}