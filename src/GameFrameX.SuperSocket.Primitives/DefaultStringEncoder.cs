using System.Buffers;
using System.Text;
using GameFrameX.SuperSocket.ProtoBase;

namespace GameFrameX.SuperSocket.Primitives
{
    public class DefaultStringEncoder : IPackageEncoder<string>
    {
        private Encoding _encoding;

        public DefaultStringEncoder()
            : this(new UTF8Encoding(false))
        {
        }

        public DefaultStringEncoder(Encoding encoding)
        {
            _encoding = encoding;
        }

        public int Encode(IBufferWriter<byte> writer, string pack)
        {
            return writer.Write(pack, _encoding);
        }
    }
}