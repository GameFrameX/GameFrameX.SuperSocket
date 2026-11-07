using System.Buffers;
using GameFrameX.SuperSocket.Connection;
using GameFrameX.SuperSocket.ProtoBase;
using Xunit;

namespace GameFrameX.SuperSocket.Connection.Tests;

/// <summary>
/// Pipe connection shutdown and pipeline error handling tests.
/// </summary>
public class PipeConnectionBaseTests
{
    /// <summary>
    /// Verifies that an exception while reading pipeline state is contained and closes the connection.
    /// </summary>
    [Fact]
    public async Task RunAsync_ShouldContainPipelineStateExceptions_AndCloseTheConnection()
    {
        await using var connection = new TestVirtualConnection();
        await using var packages = connection.RunAsync(new ThrowingNextFilter()).GetAsyncEnumerator();

        var moveNextTask = packages.MoveNextAsync().AsTask();
        await connection.WriteInputPipeDataAsync(new byte[] { 1 }, CancellationToken.None);

        Assert.False(await moveNextTask.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(connection.CloseCalled);

        await connection.CloseAsync(CloseReason.LocalClosing);
        Assert.True(connection.IsClosed);
    }

    private sealed class TestVirtualConnection : VirtualConnection
    {
        public TestVirtualConnection()
            : base(new ConnectionOptions())
        {
        }

        public bool CloseCalled { get; private set; }

        protected override void Close()
        {
            CloseCalled = true;
            Input.Writer.Complete();
        }

        protected override ValueTask<int> SendOverIOAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult((int)buffer.Length);
        }

        protected override ValueTask<int> FillInputPipeWithDataAsync(Memory<byte> memory, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class ThrowingNextFilter : IPipelineFilter<string>
    {
        public IPackageDecoder<string> Decoder { get; set; }

        public object Context { get; set; }

        public IPipelineFilter<string> NextFilter => throw new NullReferenceException("simulated filter state race");

        public string Filter(ref SequenceReader<byte> reader)
        {
            reader.AdvanceToEnd();
            return "package";
        }

        public void Reset()
        {
        }
    }
}
