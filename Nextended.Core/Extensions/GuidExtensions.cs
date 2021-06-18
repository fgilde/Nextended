using System;

namespace Nextended.Core.Extensions
{
    public static class GuidExtensions
    {
        public static int ToInt(this Guid value)
        {
            return BitConverter.ToInt32(value.ToByteArray(), 0);
        }

        public static long ToInt64(this Guid value)
        {
            return BitConverter.ToInt64(value.ToByteArray(), 0);
        }

	}
}