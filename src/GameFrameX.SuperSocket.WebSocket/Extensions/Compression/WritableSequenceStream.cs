using System.Buffers;
using GameFrameX.SuperSocket.ProtoBase;

namespace GameFrameX.SuperSocket.WebSocket.Extensions.Compression
{
    class WritableSequenceStream : Stream
    {
        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the length of the stream. Not supported.
        /// </summary>
        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets or sets the position within the stream. Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        private SequenceSegment _head;

        private SequenceSegment _tail;

        private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var data = _arrayPool.Rent(count);

            Array.Copy(buffer, offset, data, 0, count);

            var segment = new SequenceSegment(data, count);

            if (_head == null)
            {
                _tail = _head = segment;
            }
            else
            {
                _tail.SetNext(segment);
            }
        }

        public ReadOnlySequence<byte> GetUnderlyingSequence()
        {
            return new ReadOnlySequence<byte>(_head, 0, _tail, _tail.Memory.Length);
        }
    }
}