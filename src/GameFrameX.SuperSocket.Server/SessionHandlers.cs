using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.Server.Abstractions.Session;

namespace GameFrameX.SuperSocket.Server
{
    /// <summary>
    /// Represents handlers for session events such as connection and disconnection.
    /// </summary>
    public class SessionHandlers
    {
        /// <summary>
        /// Gets or sets the handler to be invoked when a session is connected.
        /// </summary>
        public Func<IAppSession, ValueTask> Connected { get; set; }

        /// <summary>
        /// Gets or sets the handler to be invoked when a session is closed.
        /// </summary>
        public Func<IAppSession, CloseEventArgs, ValueTask> Closed { get; set; }
    }
}