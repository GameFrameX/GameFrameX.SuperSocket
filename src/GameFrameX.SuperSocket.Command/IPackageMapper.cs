using System;
using System.Threading.Tasks;

namespace GameFrameX.SuperSocket.Command
{
    public interface IPackageMapper<PackageFrom, PackageTo>
    {
        PackageTo Map(PackageFrom package);
    }
}