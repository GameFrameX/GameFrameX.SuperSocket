using System.IO.Pipelines;

namespace GameFrameX.SuperSocket.Connection
{
    public interface IVirtualConnection : IConnection
    {
        ValueTask<FlushResult> WritePipeDataAsync(Memory<byte> memory, CancellationToken cancellationToken);
    }
}