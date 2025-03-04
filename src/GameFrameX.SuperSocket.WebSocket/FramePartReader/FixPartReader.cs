using System;
using System.Buffers;
using GameFrameX.SuperSocket.ProtoBase;

namespace GameFrameX.SuperSocket.WebSocket.FramePartReader
{
    /// <summary>
    /// WebSocket 固定部分读取器
    /// </summary>
    class FixPartReader : PackagePartReader
    {
        /// <summary>
        /// 处理 WebSocket 帧的固定部分
        /// </summary>
        /// <param name="package">WebSocket 数据包</param>
        /// <param name="filterContext">过滤器上下文</param>
        /// <param name="reader">序列读取器</param>
        /// <param name="nextPartReader">下一个部分读取器</param>
        /// <param name="needMoreData">是否需要更多数据</param>
        /// <returns>是否处理完成</returns>
        public override bool Process(WebSocketPackage package, object filterContext, ref SequenceReader<byte> reader, out IPackagePartReader<WebSocketPackage> nextPartReader, out bool needMoreData)
        {
            if (reader.Length < 2)
            {
                nextPartReader = null;
                needMoreData = true;
                return false;
            }

            needMoreData = false;

            reader.TryRead(out byte firstByte);

            var opCode = (OpCode)(firstByte & 0x0f);

            if (opCode != OpCode.Continuation)
            {
                package.OpCode = opCode;
            }

            package.OpCodeByte = firstByte;

            reader.TryRead(out byte secondByte);
            package.PayloadLength = secondByte & 0x7f;
            package.HasMask = (secondByte & 0x80) == 0x80;

            if (package.PayloadLength >= 126)
            {
                nextPartReader = ExtendedLengthReader;
            }
            else
            {
                if (package.HasMask)
                    nextPartReader = MaskKeyReader;
                else
                {
                    if (TryInitIfEmptyMessage(package))
                    {
                        nextPartReader = null;
                        return true;
                    }

                    nextPartReader = PayloadDataReader;
                }
            }

            return false;
        }
    }
}