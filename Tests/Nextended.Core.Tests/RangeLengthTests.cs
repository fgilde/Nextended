using Nextended.Core.Types;
using Xunit;
using Assert = Xunit.Assert;

namespace Nextended.Core.Tests
{
    public class RangeLengthTests
    {
        private static RangeLength<double> Len(double delta)
            => new RangeLength<double>(delta);

        [Fact]
        public void Constructor_SetsDelta()
        {
            var len = Len(5.0);
            Assert.Equal(5.0, len.Delta);
        }

        [Fact]
        public void AddTo_UsesMathCorrectly()
        {
            var len = Len(2.5);
            var result = len.AddTo(10.0);
            Assert.Equal(12.5, result, 5);
        }

        [Fact]
        public void SubtractFrom_UsesMathCorrectly()
        {
            var len = Len(2.5);
            var result = len.SubtractFrom(10.0);
            Assert.Equal(7.5, result, 5);
        }

        [Fact]
        public void ArithmeticOperators_WorkAsExpected()
        {
            var a = Len(5.0);
            var b = Len(3.0);

            var sum = a + b;
            var diff = a - b;
            var scaled = a * 2.0;
            var divided = a / 2.0;

            Assert.Equal(8.0, sum.Delta, 5);
            Assert.Equal(2.0, diff.Delta, 5);
            Assert.Equal(10.0, scaled.Delta, 5);
            Assert.Equal(2.5, divided.Delta, 5);
        }

        [Fact]
        public void Equality_UsesDelta()
        {
            var a = Len(5.0);
            var b = Len(5.0);
            var c = Len(4.999);

            Assert.True(a == b);
            Assert.False(a != b);

            Assert.False(a == c);
            Assert.True(a != c);

            Assert.True(a.Equals(b));
            Assert.False(a.Equals(c));
        }

        [Fact]
        public void ComparisonOperators_CompareByDelta()
        {
            var small = Len(1.0);
            var mid = Len(2.0);
            var large = Len(3.0);

            Assert.True(large > mid);
            Assert.True(mid > small);
            Assert.True(small < large);

            Assert.True(mid >= small);
            Assert.True(mid >= mid);
            Assert.True(mid <= large);
            Assert.True(mid <= mid);
        }

        [Fact]
        public void CompareTo_UsesDelta()
        {
            var a = Len(5.0);
            var b = Len(3.0);
            var c = Len(5.0);

            Assert.True(a.CompareTo(b) > 0);
            Assert.True(b.CompareTo(a) < 0);
            Assert.Equal(0, a.CompareTo(c));
        }

        [Fact]
        public void GetHashCode_BasedOnDelta()
        {
            var a = Len(5.0);
            var b = Len(5.0);

            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void ToString_ShowsDelta()
        {
            var len = Len(5.0);
            var text = len.ToString();

            Assert.Contains("5", text);
            Assert.StartsWith("Δ=", text);
        }
    }
}
