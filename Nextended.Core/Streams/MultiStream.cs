using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nextended.Core.Streams
{
    /// <summary>
    /// Stream which reads data from multiple streams in sequence until all are exhausted.
    /// Supports streams which are not seekable or have an undefined length, e.g HTTP streams.
    /// </summary>
    public class MultiStream : Stream
    {
        private readonly bool _disposeSourceStreams;
        private IList<MultiStreamPointer> _streams;
        private long _totalPosition = 0;

        /// <summary>
        /// Creates a new instance of <see cref="MultiStream"/>
        /// </summary>
        /// <param name="sourceStreams">The stream to generate a hash value for</param>
        /// <param name="disposeSourceStreams">Indicates whether the source streams should be disposed together with the <see cref="MultiStream"/></param>
        public MultiStream(IList<Stream> sourceStreams, bool disposeSourceStreams = true)
        {
            _disposeSourceStreams = disposeSourceStreams;
            _streams = new List<MultiStreamPointer>(sourceStreams.Select(s => new MultiStreamPointer {Stream = s}));
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = 0;
            var currBuffer = new byte[count];
            var remaining = count;

            if (_streams.Any(s => !s.IsExhausted))
            {
                do
                {
                    var streamPointer = _streams.FirstOrDefault(s => !s.IsExhausted);
                    if (streamPointer == null)
                    {
                        break;
                    }

                    var currentRead = await streamPointer.Stream.ReadAsync(currBuffer, 0, remaining, cancellationToken);
                    streamPointer.Position += currentRead;
                    remaining -= currentRead;

                    if (currentRead == 0)
                    {
                        streamPointer.IsExhausted = true;
                    }
                    else
                    {
                        // Append the data that has been read to the input buffer
                        Buffer.BlockCopy(currBuffer, 0, buffer, read, currentRead);
                    }
                    
                    read += currentRead;
                } while (remaining > 0);
            }
      
            _totalPosition += read;
        
            return read;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
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
            throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => _totalPosition;
            set => throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_disposeSourceStreams && _streams != null)
                {
                    foreach (var unreadSourceStream in _streams)
                    {
                        unreadSourceStream?.Stream.Dispose();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
                _streams = null;
            }
        }
        
        private class MultiStreamPointer
        {
            public Stream Stream { get; set; }
            public int Position { get; set; }
            public bool IsExhausted { get; set; }
        }
    }
}