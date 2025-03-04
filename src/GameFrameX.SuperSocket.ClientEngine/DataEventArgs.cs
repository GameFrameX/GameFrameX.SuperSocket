using System;

namespace GameFrameX.SuperSocket.ClientEngine
{
    /// <summary>
    /// 数据事件参数类，用于处理数据传输相关的事件
    /// </summary>
    public class DataEventArgs : EventArgs
    {
        /// <summary>
        /// 获取或设置数据字节数组
        /// </summary>
        /// <value>包含传输数据的字节数组</value>
        public byte[] Data { get; set; }

        /// <summary>
        /// 获取或设置数据的偏移量
        /// </summary>
        /// <value>数据在缓冲区中的起始位置</value>
        public int Offset { get; set; }

        /// <summary>
        /// 获取或设置数据的长度
        /// </summary>
        /// <value>要处理的数据长度</value>
        public int Length { get; set; }
    }
}