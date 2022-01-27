using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nextended.Core.Streams
{
    /// <summary>
    /// Stream to wrap another stream to prevent it from being disposed
    /// </summary>
    public class NonDisposableStream : Stream
    {
        private Stream _sourceStream;

        public NonDisposableStream(Stream sourceStream)
        {
            _sourceStream = sourceStream;
        }

        public override void Flush()
        {
            _sourceStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _sourceStream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _sourceStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _sourceStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _sourceStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _sourceStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _sourceStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _sourceStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override bool CanRead => _sourceStream.CanRead;
        public override bool CanSeek => _sourceStream.CanSeek;
        public override bool CanWrite => _sourceStream.CanWrite;
        public override long Length => _sourceStream.Length;
        public override long Position
        {
            get => _sourceStream.Position;
            set => _sourceStream.Position = value;
        }
        
        protected override void Dispose(bool disposing)
        {
            // Explicitly do nothing here
        }

        public void ForceDispose()
        {
            try
            {
                try
                {
                    Flush();
                }
                finally
                {
                    _sourceStream?.Dispose();
                }
            }
            finally
            {
                base.Dispose();
                _sourceStream = null;
            }
        }
    }
}