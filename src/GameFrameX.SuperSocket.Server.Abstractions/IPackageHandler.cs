using System;
using System.Threading;
using System.Threading.Tasks;
using GameFrameX.SuperSocket.Server.Abstractions.Session;

namespace GameFrameX.SuperSocket.Server.Abstractions
{
    public interface IPackageHandler<TReceivePackageInfo>
    {
        ValueTask Handle(IAppSession session, TReceivePackageInfo package, CancellationToken cancellationToken);
    }
}