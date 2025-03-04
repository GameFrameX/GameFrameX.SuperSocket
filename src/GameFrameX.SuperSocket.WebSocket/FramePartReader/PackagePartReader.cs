using System;
using System.Buffers;
using GameFrameX.SuperSocket.ProtoBase;


namespace GameFrameX.SuperSocket.WebSocket.FramePartReader
{
    /// <summary>
    /// WebSocket 数据包部分读取器基类
    /// </summary>
    abstract class PackagePartReader : IPackagePartReader<WebSocketPackage>
    {
        /// <summary>
        /// 获取新的读取器实例
        /// </summary>
        public static IPackagePartReader<WebSocketPackage> NewReader
        {
            get { return FixPartReader; }
        }
    
        /// <summary>
        /// 固定部分读取器
        /// </summary>
        protected static IPackagePartReader<WebSocketPackage> FixPartReader { get; private set; }
    
        /// <summary>
        /// 扩展长度读取器
        /// </summary>
        protected static IPackagePartReader<WebSocketPackage> ExtendedLengthReader { get; private set; }
    
        /// <summary>
        /// 掩码密钥读取器
        /// </summary>
        protected static IPackagePartReader<WebSocketPackage> MaskKeyReader { get; private set; }
    
        /// <summary>
        /// 负载数据读取器
        /// </summary>
        protected static IPackagePartReader<WebSocketPackage> PayloadDataReader { get; private set; }
    
        static PackagePartReader()
        {
            FixPartReader = new FixPartReader();
            ExtendedLengthReader = new ExtendedLengthReader();
            MaskKeyReader = new MaskKeyReader();
            PayloadDataReader = new PayloadDataReader();
        }
    
        public abstract bool Process(WebSocketPackage package, object filterContext, ref SequenceReader<byte> reader, out IPackagePartReader<WebSocketPackage> nextPartReader, out bool needMoreData);
    
        protected bool TryInitIfEmptyMessage(WebSocketPackage package)
        {
            if (package.PayloadLength != 0)
                return false;
    
            if (package.OpCode == OpCode.Text)
                package.Message = string.Empty;
    
            return true;
        }
    }
}