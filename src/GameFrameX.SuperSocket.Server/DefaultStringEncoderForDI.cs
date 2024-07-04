using System.Text;
using GameFrameX.SuperSocket.Primitives;
using GameFrameX.SuperSocket.Server.Abstractions;
using Microsoft.Extensions.Options;

namespace GameFrameX.SuperSocket.Server
{
    class DefaultStringEncoderForDI : DefaultStringEncoder
    {
        public DefaultStringEncoderForDI(IOptions<ServerOptions> serverOptions)
            : base(serverOptions.Value?.DefaultTextEncoding ?? new UTF8Encoding(false))
        {
        }
    }
}