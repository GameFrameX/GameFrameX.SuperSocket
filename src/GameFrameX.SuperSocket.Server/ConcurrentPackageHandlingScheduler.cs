using GameFrameX.SuperSocket.Primitives;
using GameFrameX.SuperSocket.Server.Abstractions.Session;

namespace GameFrameX.SuperSocket.Server
{
    public class ConcurrentPackageHandlingScheduler<TPackageInfo> : PackageHandlingSchedulerBase<TPackageInfo>
    {
        public override ValueTask HandlePackage(IAppSession session, TPackageInfo package, CancellationToken cancellationToken)
        {
            HandlePackageInternal(session, package, cancellationToken).DoNotAwait();
            return new ValueTask();
        }
    }
}