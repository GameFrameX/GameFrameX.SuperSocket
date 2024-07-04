using System.Collections.Specialized;
using GameFrameX.SuperSocket.WebSocket.Extensions;

namespace GameFrameX.SuperSocket.WebSocket.Server.Extensions;

public interface IWebSocketExtensionFactory
{
    string Name { get; }

    IWebSocketExtension Create(NameValueCollection options, out NameValueCollection supportedOptions);
}