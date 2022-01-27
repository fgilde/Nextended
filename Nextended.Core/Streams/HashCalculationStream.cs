using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Nextended.Core.Streams
{
    /// <summary>
    /// Stream which calculates a hash value while the data is being read from the wrapped stream.
    /// </summary>
    public class HashCalculationStream : Stream
    {
        private Stream _sourceStream;
        private readonly bool _disposeSourceStream;
        private IncrementalHash _hash;

        /// <summary>
        /// Creates a new instance of <see cref="HashCalculationStream"/>
        /// </summary>
        /// <param name="sourceStream">The stream to generate a hash value for</param>
        /// <param name="hashAlgorithmName">The hash algorithm to use</param>
        /// <param name="disposeSourceStream">Indicates whether the source stream should be disposed together with the <see cref="HashCalculationStream"/></param>
        public HashCalculationStream(Stream sourceStream, HashAlgorithmName hashAlgorithmName, bool disposeSourceStream = true)
        {
            _sourceStream = sourceStream;
            _disposeSourceStream = disposeSourceStream;
            _hash = IncrementalHash.CreateHash(hashAlgorithmName);
        }

        /// <summary>
        /// Call this to retrieve the hash after the stream has been fully read but before it is disposed.
        /// </summary>
        /// <returns></returns>
        public byte[] GetHash()
        {
            return _hash.GetHashAndReset();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = await _sourceStream.ReadAsync(buffer, offset, count, cancellationToken);
            if (read != 0)
            {
                // Incrementally calculate the hash by feeding the read bytes into it
                _hash.AppendData(buffer, offset, read);
            }
            
            return read;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _sourceStream.Seek(offset, origin);
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
            try
            {
                try
                {
                    Flush();
                }
                finally
                {
                    if (_disposeSourceStream)
                    {
                        _sourceStream?.Dispose();    
                    }
                    
                    _hash?.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
                _sourceStream = null;
                _hash = null;
            }
        }
    }
}