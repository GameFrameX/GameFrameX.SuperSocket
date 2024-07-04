using Microsoft.Extensions.Logging;

namespace GameFrameX.SuperSocket.Primitives
{
    public interface ILoggerAccessor
    {
        ILogger Logger { get; }
    }
}