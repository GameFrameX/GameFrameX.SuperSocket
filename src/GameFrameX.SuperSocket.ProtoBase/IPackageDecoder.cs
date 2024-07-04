using System.Buffers;

namespace GameFrameX.SuperSocket.ProtoBase
{
    public interface IPackageDecoder<out TPackageInfo>
    {
        TPackageInfo Decode(ref ReadOnlySequence<byte> buffer, object context);
    }
}