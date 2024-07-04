namespace GameFrameX.SuperSocket.Connection
{
    public interface IConnectionFactory
    {
        Task<IConnection> CreateConnection(object connection, CancellationToken cancellationToken);
    }
}