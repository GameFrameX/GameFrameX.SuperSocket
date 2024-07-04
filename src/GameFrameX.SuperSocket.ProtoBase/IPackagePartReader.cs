using System.Buffers;

namespace GameFrameX.SuperSocket.ProtoBase
{
    public interface IPackagePartReader<TPackageInfo>
    {
        bool Process(TPackageInfo package, object filterContext, ref SequenceReader<byte> reader, out IPackagePartReader<TPackageInfo> nextPartReader, out bool needMoreData);
    }
}