using System.Buffers;

namespace GameFrameX.SuperSocket.ProtoBase
{
    public interface IPackageEncoder<in TPackageInfo>
    {
        int Encode(IBufferWriter<byte> writer, TPackageInfo pack);
    }
}