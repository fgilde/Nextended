using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nextended.Core.Extensions
{
    public static class StreamExtensions
    {
        public static byte[] ToByteArray(this Stream input)
        {
            if (input == null)
            {
                return null;
            }
            if (input is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }
            using var ms = new MemoryStream();
            input.Position = 0;
            input.Seek(0, SeekOrigin.Begin);
            input.CopyToAsync(ms);
            return ms.ToArray();
        }
    }
}
