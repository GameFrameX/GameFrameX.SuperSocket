using System.Collections.Specialized;
using GameFrameX.SuperSocket.WebSocket.Extensions;

namespace GameFrameX.SuperSocket.WebSocket.Server.Extensions;

public interface IWebSocketExtensionFactory
{
    /// <summary>
    /// The name of the WebSocket extension.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Creates a new instance of the WebSocket extension.
    /// </summary>
    /// <param name="options">The options for the WebSocket extension.</param>
    /// <param name="supportedOptions">The supported options for the WebSocket extension.</param>
    /// <returns>The created WebSocket extension.</returns>
    IWebSocketExtension Create(NameValueCollection options, out NameValueCollection supportedOptions);
}