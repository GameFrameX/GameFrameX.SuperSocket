using Microsoft.Extensions.Logging;

namespace GameFrameX.SuperSocket.Primitives
{
    /// <summary>
    /// Provides access to an <see cref="ILogger"/> instance.
    /// </summary>
    public interface ILoggerAccessor
    {
        /// <summary>
        /// Gets the <see cref="ILogger"/> instance.
        /// </summary>
        ILogger Logger { get; }
    }
}