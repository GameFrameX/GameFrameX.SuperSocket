using System.Collections.Specialized;
using GameFrameX.SuperSocket.WebSocket.Extensions;

namespace GameFrameX.SuperSocket.WebSocket.Server.Extensions;

/// <summary>
/// Defines a factory for creating WebSocket extensions.
/// </summary>
public interface IWebSocketExtensionFactory
{
    /// <summary>
    /// Gets the name of the WebSocket extension.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Creates a WebSocket extension based on the specified options.
    /// </summary>
    /// <param name="options">The options for the extension.</param>
    /// <param name="supportedOptions">The supported options for the extension.</param>
    /// <returns>The created WebSocket extension.</returns>
    IWebSocketExtension Create(NameValueCollection options, out NameValueCollection supportedOptions);
}