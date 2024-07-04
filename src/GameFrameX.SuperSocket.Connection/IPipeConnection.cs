using System.IO.Pipelines;
using GameFrameX.SuperSocket.ProtoBase;

namespace GameFrameX.SuperSocket.Connection
{
    public interface IPipeConnection
    {
        IPipelineFilter PipelineFilter { get; }

        PipeReader InputReader { get; }

        PipeWriter OutputWriter { get; }
    }
}