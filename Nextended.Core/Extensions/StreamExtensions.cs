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
            using var ms = new MemoryStream();
            input.CopyToAsync(ms);
            return ms.ToArray();
        }
    }
}
