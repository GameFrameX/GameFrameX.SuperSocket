using System.Collections.Specialized;
using GameFrameX.SuperSocket.WebSocket.Extensions;
using GameFrameX.SuperSocket.WebSocket.Extensions.Compression;

namespace GameFrameX.SuperSocket.WebSocket.Server.Extensions.Compression;

/// <summary>
///     WebSocket Per-Message Compression Extension
///     https://tools.ietf.org/html/rfc7692
/// </summary>
public class WebSocketPerMessageCompressionExtensionFactory : IWebSocketExtensionFactory
{
    private static readonly NameValueCollection _supportedOptions;

    static WebSocketPerMessageCompressionExtensionFactory()
    {
        _supportedOptions = new NameValueCollection();
        _supportedOptions.Add("client_no_context_takeover", string.Empty);
    }

    public string Name
    {
        get { return WebSocketPerMessageCompressionExtension.PMCE; }
    }

    public IWebSocketExtension Create(NameValueCollection options, out NameValueCollection supportedOptions)
    {
        supportedOptions = _supportedOptions;

        if (options != null && options.Count > 0)
        {
            foreach (var key in options.AllKeys)
            {
                if (key.StartsWith("server_", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(options.Get(key)))
                        return null;
                }
            }
        }

        return new WebSocketPerMessageCompressionExtension();
    }
}