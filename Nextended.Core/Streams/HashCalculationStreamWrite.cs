using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Nextended.Core.Streams
{
    /// <summary>
    /// Stream which calculates a hash value while the data is being written to it.
    /// Useful to calculate a hash for a stream but not actually persist it somewhere.
    /// </summary>
    public class HashCalculationStreamWrite : Stream
    {
        private long _bytesWritten;
        private long _position;
        private IncrementalHash _hash;

        /// <summary>
        /// Creates a new instance of <see cref="HashCalculationStreamWrite"/>
        /// </summary>
        /// <param name="hashAlgorithmName">The hash algorithm to use</param>
        public HashCalculationStreamWrite(HashAlgorithmName hashAlgorithmName)
        {
            _hash = IncrementalHash.CreateHash(hashAlgorithmName);
        }

        /// <summary>
        /// Call this to retrieve the hash after the stream has been fully written but before it is disposed.
        /// </summary>
        /// <returns></returns>
        public byte[] GetHash()
        {
            return _hash.GetHashAndReset();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // Incrementally calculate the hash by feeding the bytes into it
            _hash.AppendData(buffer, offset, count);
            _bytesWritten += count;
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

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _bytesWritten;

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                _hash?.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
                _hash = null;
            }
        }
    }
}