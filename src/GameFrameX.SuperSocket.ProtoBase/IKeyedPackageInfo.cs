using System;

namespace GameFrameX.SuperSocket.ProtoBase
{
    public interface IKeyedPackageInfo<TKey>
    {
        TKey Key { get; }
    }
}