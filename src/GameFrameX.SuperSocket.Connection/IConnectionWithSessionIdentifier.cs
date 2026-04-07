namespace GameFrameX.SuperSocket.Connection
{
    /// <summary>
    /// Represents a connection that includes a unique session identifier.
    /// </summary>
    public interface IConnectionWithSessionIdentifier
    {
        /// <summary>
        /// Gets the unique identifier for the connection session.
        /// </summary>
        string SessionIdentifier { get; }
    }
}