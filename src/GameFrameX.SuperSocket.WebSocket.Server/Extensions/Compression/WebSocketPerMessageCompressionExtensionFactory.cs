using System.Collections.Specialized;
using GameFrameX.SuperSocket.WebSocket.Extensions;
using GameFrameX.SuperSocket.WebSocket.Extensions.Compression;

namespace GameFrameX.SuperSocket.WebSocket.Server.Extensions.Compression;

/// <summary>
///     WebSocket Per-Message Compression Extension
///     https://tools.ietf.org/html/rfc7692
/// </summary>
/// <remarks>
/// WebSocket消息压缩扩展工厂类，用于创建和管理WebSocket消息压缩扩展
/// </remarks>
public class WebSocketPerMessageCompressionExtensionFactory : IWebSocketExtensionFactory
{
    /// <summary>
    /// 支持的压缩选项集合
    /// </summary>
    private static readonly NameValueCollection _supportedOptions;

    /// <summary>
    /// 静态构造函数，初始化支持的压缩选项
    /// </summary>
    static WebSocketPerMessageCompressionExtensionFactory()
    {
        _supportedOptions = new NameValueCollection();
        _supportedOptions.Add("client_no_context_takeover", string.Empty);
    }

    /// <summary>
    /// 获取扩展名称
    /// </summary>
    public string Name => WebSocketPerMessageCompressionExtension.PMCE;

    /// <summary>
    /// 创建WebSocket压缩扩展实例
    /// </summary>
    /// <param name="options">客户端请求的压缩选项</param>
    /// <param name="supportedOptions">服务器支持的压缩选项</param>
    /// <returns>返回WebSocket压缩扩展实例，如果选项无效则返回null</returns>
    public IWebSocketExtension Create(NameValueCollection options, out NameValueCollection supportedOptions)
    {
        supportedOptions = _supportedOptions;

        if (options != null && options.Count > 0)
            foreach (var key in options.AllKeys)
                if (key.StartsWith("server_", StringComparison.OrdinalIgnoreCase))
                    if (!string.IsNullOrEmpty(options.Get(key)))
                        return null;

        return new WebSocketPerMessageCompressionExtension();
    }
}