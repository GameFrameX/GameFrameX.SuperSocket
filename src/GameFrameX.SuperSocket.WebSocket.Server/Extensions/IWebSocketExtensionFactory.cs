using System.Collections.Specialized;
using GameFrameX.SuperSocket.WebSocket.Extensions;

namespace GameFrameX.SuperSocket.WebSocket.Server.Extensions;

/// <summary>
/// WebSocket 扩展工厂接口
/// </summary>
public interface IWebSocketExtensionFactory
{
    /// <summary>
    /// 获取扩展名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 创建 WebSocket 扩展实例
    /// </summary>
    /// <param name="options">客户端请求的扩展选项</param>
    /// <param name="supportedOptions">服务器支持的扩展选项</param>
    /// <returns>WebSocket 扩展实例，如果不支持请求的选项则返回 null</returns>
    IWebSocketExtension Create(NameValueCollection options, out NameValueCollection supportedOptions);
}