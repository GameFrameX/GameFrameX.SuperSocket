using System;
using System.Buffers;
using System.Linq;
using GameFrameX.SuperSocket.ProtoBase;

namespace GameFrameX.SuperSocket.WebSocket.FramePartReader
{
    /// <summary>
    /// WebSocket 掩码密钥读取器
    /// </summary>
    class MaskKeyReader : PackagePartReader
    {
        /// <summary>
        /// 处理 WebSocket 掩码密钥
        /// </summary>
        /// <param name="package">WebSocket 数据包</param>
        /// <param name="filterContext">过滤器上下文</param>
        /// <param name="reader">序列读取器</param>
        /// <param name="nextPartReader">下一个部分读取器</param>
        /// <param name="needMoreData">是否需要更多数据</param>
        /// <returns>是否处理完成</returns>
        public override bool Process(WebSocketPackage package, object filterContext, ref SequenceReader<byte> reader, out IPackagePartReader<WebSocketPackage> nextPartReader, out bool needMoreData)
        {
            int required = 4;

            if (reader.Remaining < required)
            {
                nextPartReader = null;
                needMoreData = true;
                return false;
            }

            needMoreData = false;

            package.MaskKey = reader.Sequence.Slice(reader.Consumed, 4).ToArray();
            reader.Advance(4);

            if (TryInitIfEmptyMessage(package))
            {
                nextPartReader = null;
                return true;
            }

            nextPartReader = PayloadDataReader;
            return false;
        }
    }
}