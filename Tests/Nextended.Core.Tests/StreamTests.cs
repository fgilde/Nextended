using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Streams;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class StreamTests
	{
		[TestMethod]
		public async Task MultiStream_ReadAsync_ReadsSingleStream()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Hello World"));
			var multiStream = new MultiStream(new List<Stream> { stream1 });
			
			var buffer = new byte[100];
			var bytesRead = await multiStream.ReadAsync(buffer, 0, buffer.Length);
			
			var result = Encoding.UTF8.GetString(buffer, 0, bytesRead);
			Assert.AreEqual("Hello World", result);
			Assert.AreEqual(11, bytesRead);
		}

		[TestMethod]
		public async Task MultiStream_ReadAsync_ReadsMultipleStreamsInSequence()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Hello "));
			var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("World"));
			var multiStream = new MultiStream(new List<Stream> { stream1, stream2 });
			
			var buffer = new byte[100];
			var bytesRead = await multiStream.ReadAsync(buffer, 0, buffer.Length);
			
			var result = Encoding.UTF8.GetString(buffer, 0, bytesRead);
			Assert.AreEqual("Hello World", result);
			Assert.AreEqual(11, bytesRead);
		}

		[TestMethod]
		public async Task MultiStream_ReadAsync_EmptyStream_ReturnsZero()
		{
			var stream1 = new MemoryStream();
			var multiStream = new MultiStream(new List<Stream> { stream1 });
			
			var buffer = new byte[100];
			var bytesRead = await multiStream.ReadAsync(buffer, 0, buffer.Length);
			
			Assert.AreEqual(0, bytesRead);
		}

		[TestMethod]
		public async Task MultiStream_ReadAsync_PartialReads_WorksCorrectly()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Hello"));
			var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(" World"));
			var multiStream = new MultiStream(new List<Stream> { stream1, stream2 });
			
			var buffer = new byte[5];
			var bytesRead1 = await multiStream.ReadAsync(buffer, 0, buffer.Length);
			var result1 = Encoding.UTF8.GetString(buffer, 0, bytesRead1);
			
			var bytesRead2 = await multiStream.ReadAsync(buffer, 0, buffer.Length);
			var result2 = Encoding.UTF8.GetString(buffer, 0, bytesRead2);
			
			Assert.AreEqual("Hello", result1);
			Assert.AreEqual(" Worl", result2);
		}

		[TestMethod]
		public void MultiStream_Read_SynchronousRead_Works()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
			var multiStream = new MultiStream(new List<Stream> { stream1 });
			
			var buffer = new byte[100];
			var bytesRead = multiStream.Read(buffer, 0, buffer.Length);
			
			var result = Encoding.UTF8.GetString(buffer, 0, bytesRead);
			Assert.AreEqual("Test", result);
		}

		[TestMethod]
		public void MultiStream_Properties_CorrectValues()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
			var multiStream = new MultiStream(new List<Stream> { stream1 });
			
			Assert.IsTrue(multiStream.CanRead);
			Assert.IsFalse(multiStream.CanSeek);
			Assert.IsFalse(multiStream.CanWrite);
		}

		[TestMethod]
		public void MultiStream_Position_ReturnsCorrectPosition()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
			var multiStream = new MultiStream(new List<Stream> { stream1 });
			
			Assert.AreEqual(0, multiStream.Position);
			
			var buffer = new byte[2];
			multiStream.Read(buffer, 0, buffer.Length);
			
			Assert.AreEqual(2, multiStream.Position);
		}

		[TestMethod]
		public void MultiStream_Seek_ThrowsNotSupportedException()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
			var multiStream = new MultiStream(new List<Stream> { stream1 });
			
			ExceptionAssert.Throws<NotSupportedException>(
				() => multiStream.Seek(0, SeekOrigin.Begin));
		}

		[TestMethod]
		public void MultiStream_SetLength_ThrowsNotSupportedException()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
			var multiStream = new MultiStream(new List<Stream> { stream1 });
			
			ExceptionAssert.Throws<NotSupportedException>(
				() => multiStream.SetLength(10));
		}

		[TestMethod]
		public void MultiStream_Write_ThrowsNotSupportedException()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
			var multiStream = new MultiStream(new List<Stream> { stream1 });
			
			ExceptionAssert.Throws<NotSupportedException>(
				() => multiStream.Write(new byte[10], 0, 10));
		}

		[TestMethod]
		public void MultiStream_GetLength_ThrowsNotSupportedException()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
			var multiStream = new MultiStream(new List<Stream> { stream1 });
			
			ExceptionAssert.Throws<NotSupportedException>(
				() => { var _ = multiStream.Length; });
		}

		[TestMethod]
		public void MultiStream_SetPosition_ThrowsNotSupportedException()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
			var multiStream = new MultiStream(new List<Stream> { stream1 });
			
			ExceptionAssert.Throws<NotSupportedException>(
				() => multiStream.Position = 0);
		}

		[TestMethod]
		public void MultiStream_Flush_DoesNotThrow()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
			var multiStream = new MultiStream(new List<Stream> { stream1 });
			
			multiStream.Flush();
			// Should not throw
			Assert.IsTrue(true);
		}

		[TestMethod]
		public async Task MultiStream_FlushAsync_DoesNotThrow()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
			var multiStream = new MultiStream(new List<Stream> { stream1 });
			
			await multiStream.FlushAsync();
			// Should not throw
			Assert.IsTrue(true);
		}

		[TestMethod]
		public void MultiStream_Dispose_DisposesSourceStreams()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
			var multiStream = new MultiStream(new List<Stream> { stream1 }, disposeSourceStreams: true);
			
			multiStream.Dispose();
			
			// Trying to access disposed stream should throw
			ExceptionAssert.Throws<ObjectDisposedException>(
				() => stream1.ReadByte());
		}

		[TestMethod]
		public void MultiStream_Dispose_DoesNotDisposeSourceStreamsWhenDisabled()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
			var multiStream = new MultiStream(new List<Stream> { stream1 }, disposeSourceStreams: false);
			
			multiStream.Dispose();
			
			// Stream should still be accessible
			var result = stream1.ReadByte();
			Assert.AreEqual((byte)'T', result);
		}

		[TestMethod]
		public async Task MultiStream_ReadAsync_ThreeStreams_ConcatenatesCorrectly()
		{
			var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("One"));
			var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("Two"));
			var stream3 = new MemoryStream(Encoding.UTF8.GetBytes("Three"));
			var multiStream = new MultiStream(new List<Stream> { stream1, stream2, stream3 });
			
			var buffer = new byte[100];
			var bytesRead = await multiStream.ReadAsync(buffer, 0, buffer.Length);
			
			var result = Encoding.UTF8.GetString(buffer, 0, bytesRead);
			Assert.AreEqual("OneTwoThree", result);
		}

		[TestMethod]
		public void NonDisposableStream_Wrap_PreventsDisposal()
		{
			var innerStream = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
			var nonDisposable = new NonDisposableStream(innerStream);
			
			nonDisposable.Dispose();
			
			// Inner stream should still be accessible
			Assert.AreEqual(0, innerStream.Position);
			innerStream.Position = 0;
			var result = innerStream.ReadByte();
			Assert.AreEqual((byte)'T', result);
		}

		[TestMethod]
		public void NonDisposableStream_Read_ReadsFromInnerStream()
		{
			var innerStream = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
			var nonDisposable = new NonDisposableStream(innerStream);
			
			var buffer = new byte[4];
			var bytesRead = nonDisposable.Read(buffer, 0, buffer.Length);
			
			Assert.AreEqual(4, bytesRead);
			Assert.AreEqual("Test", Encoding.UTF8.GetString(buffer));
		}

		[TestMethod]
		public void NonDisposableStream_Properties_MatchInnerStream()
		{
			var innerStream = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
			var nonDisposable = new NonDisposableStream(innerStream);
			
			Assert.AreEqual(innerStream.CanRead, nonDisposable.CanRead);
			Assert.AreEqual(innerStream.CanSeek, nonDisposable.CanSeek);
			Assert.AreEqual(innerStream.CanWrite, nonDisposable.CanWrite);
			Assert.AreEqual(innerStream.Length, nonDisposable.Length);
		}
	}
}
