using System;

namespace Nextended.Core.Extensions
{
    public static class NumericExtensions
    {
        public static decimal Absolute(this decimal input)
        {
            return Math.Abs(input);
        }

        public static int Absolute(this int input)
        {
            return Math.Abs(input);
        }

        public static decimal RoundToMoney(this decimal val)
        {
            return Math.Round(val, 2);
        }

        public static decimal RoundTo(this decimal val, int place)
        {
            return Math.Round(val, place);
        }

        public static decimal Round(this decimal d, int precision)
        {
            return Math.Round(d, precision, MidpointRounding.AwayFromZero);
        }

        public static double Round(this double d, int precision)
        {
            return Math.Round(d, precision, MidpointRounding.AwayFromZero);
        }

        public static bool Between(this int value, int left, int right)
        {
            return (value >= Math.Min(left, right) && value <= Math.Max(left, right));
        }

        public static double Floor(this double d, int precision)
        {
            var multiplier = Convert.ToDecimal(Math.Pow(10, precision));
            return (double)(Math.Floor(Convert.ToDecimal(d) * multiplier) / multiplier);
        }

        public static double Ceiling(this double d, int precision)
        {
            var multiplier = Math.Pow(10, precision);
            return Math.Ceiling(d * multiplier) / multiplier;
        }

        public static Guid ToGuid(this int value)
        {
            byte[] bytes = new byte[16];
            BitConverter.GetBytes(value).CopyTo(bytes, 0);
            return new Guid(bytes);
        }

        public static Guid ToGuid(this long value)
        {
            byte[] bytes = new byte[16];
            BitConverter.GetBytes(value).CopyTo(bytes, 0);
            return new Guid(bytes);
        }
    }
}