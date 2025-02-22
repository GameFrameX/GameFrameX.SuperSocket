using System.IO.Pipelines;

namespace GameFrameX.SuperSocket.Connection
{
    public abstract class VirtualConnection : PipeConnection, IVirtualConnection
    {
        public VirtualConnection(ConnectionOptions options)
            : base(options)
        {
        }

        internal override Task FillPipeAsync(PipeWriter writer, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async ValueTask<FlushResult> WritePipeDataAsync(Memory<byte> memory, CancellationToken cancellationToken)
        {
            return await Input.Writer.WriteAsync(memory, cancellationToken).ConfigureAwait(false);
        }
    }
}