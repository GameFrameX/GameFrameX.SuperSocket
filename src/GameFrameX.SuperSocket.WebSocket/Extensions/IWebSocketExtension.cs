using System;
using System.Buffers;

namespace GameFrameX.SuperSocket.WebSocket.Extensions
{
    /// <summary>
    /// WebSocket扩展接口
    /// 参考RFC6455规范第9节：https://tools.ietf.org/html/rfc6455#section-9
    /// </summary>
    public interface IWebSocketExtension
    {
        /// <summary>
        /// 获取WebSocket扩展的名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 对WebSocket数据包进行编码处理
        /// </summary>
        /// <param name="package">需要编码的WebSocket数据包</param>
        void Encode(WebSocketPackage package);

        /// <summary>
        /// 对WebSocket数据包进行解码处理
        /// </summary>
        /// <param name="package">需要解码的WebSocket数据包</param>
        void Decode(WebSocketPackage package);
    }
}