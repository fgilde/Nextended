using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace Nextended.Core.Extensions;

public static class HttpClientExtensions
{
    public static async Task<Stream> GetStreamInTaskChunksAsync(this HttpClient httpClient, string url, int taskCount)
    {
        if (taskCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(taskCount), "Task count must be greater than zero.");

        var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
        var headResponse = await httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead);
        long totalChunkSizeInBytes = headResponse.Content.Headers.ContentLength ?? throw new InvalidOperationException("Cannot determine file size."); ;
        long chunkSizePerTask = totalChunkSizeInBytes / taskCount;

        return await httpClient.GetStreamInChunksAsync(url, chunkSizePerTask);
    }


    public static async Task<Stream> GetStreamInChunksAsync(this HttpClient httpClient, string url, long chunkSizeInBytes)
    {
        var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
        var headResponse = await httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead);

        bool supportsChunks = headResponse.Headers.AcceptRanges.Contains("bytes");
        long totalFileSize = headResponse.Content.Headers.ContentLength ?? throw new InvalidOperationException("Cannot determine file size.");

        var chunkStreams = new List<MemoryStream>();

        if (supportsChunks && totalFileSize > chunkSizeInBytes)
        {
            int chunkCount = (int)Math.Ceiling((double)totalFileSize / chunkSizeInBytes);
            var downloadTasks = new List<Task>();

            for (int i = 0; i < chunkCount; i++)
            {
                long start = i * chunkSizeInBytes;
                long end = Math.Min(start + chunkSizeInBytes - 1, totalFileSize - 1);

                var chunkStream = new MemoryStream();
                chunkStreams.Add(chunkStream);
                downloadTasks.Add(DownloadChunkAsync(httpClient, url, chunkStream, start, end));
            }
            await Task.WhenAll(downloadTasks);
        }
        else
        {
            using var contentStream = await httpClient.GetStreamAsync(url);
            int bytesRead;
            var buffer = new byte[chunkSizeInBytes];
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                var chunkStream = new MemoryStream();
                chunkStream.Write(buffer, 0, bytesRead);
                chunkStream.Seek(0, SeekOrigin.Begin);
                chunkStreams.Add(chunkStream);
            }
        }

        return new CompositeStream(chunkStreams);
    }

    private static async Task DownloadChunkAsync(HttpClient httpClient, string url, MemoryStream chunkStream, long start, long end)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(start, end);

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var contentStream = await response.Content.ReadAsStreamAsync();
        await contentStream.CopyToAsync(chunkStream);
        chunkStream.Seek(0, SeekOrigin.Begin);
    }

    /// <summary>
    /// Ein zusammengesetzter Stream, der mehrere Streams zu einem zusammenführt.
    /// </summary>
    public class CompositeStream : Stream
    {
        private readonly Queue<Stream> _streams;
        private Stream _currentStream;

        public CompositeStream(IEnumerable<Stream> streams)
        {
            _streams = new Queue<Stream>(streams);
            _currentStream = _streams.Dequeue();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _currentStream.Read(buffer, offset, count);

            while (bytesRead == 0 && _streams.Count > 0)
            {
                _currentStream = _streams.Dequeue();
                bytesRead = _currentStream.Read(buffer, offset, count);
            }

            return bytesRead;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var stream in _streams)
                {
                    stream.Dispose();
                }
                _currentStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }

}