using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Types;
using System;
using Xunit;
using Assert = Xunit.Assert;

namespace Nextended.Core.Tests
{
    [TestClass]
    public class RangeOfDblTests
    {

        [Fact]
        public void Constructor_Throws_WhenStartGreaterThanEnd()
        {
            Assert.Throws<ArgumentException>(() => new RangeOf<double>(10.0, 5.0));
        }

        [Fact]
        public void Constructor_StoresStartAndEnd()
        {
            var range = new RangeOf<double>(1.0, 5.0);

            Assert.Equal(1.0, range.Start);
            Assert.Equal(5.0, range.End);
        }

        [Theory]
        [InlineData(1.0, 5.0, 1.0, true)]
        [InlineData(1.0, 5.0, 3.0, true)]
        [InlineData(1.0, 5.0, 5.0, true)]
        [InlineData(1.0, 5.0, 0.99, false)]
        [InlineData(1.0, 5.0, 5.01, false)]
        public void Contains_TestsInclusiveBounds(double start, double end, double value, bool expected)
        {
            var range = new RangeOf<double>(start, end);
            Assert.Equal(expected, range.Contains(value));
            Assert.Equal(expected, range.IsInRange(value));
        }

        [Fact]
        public void Intersects_OverlappingRanges()
        {
            var a = new RangeOf<double>(0, 10);
            var b = new RangeOf<double>(5, 15);

            Assert.True(a.Intersects(b));
            Assert.True(b.Intersects(a));
        }

        [Fact]
        public void Intersects_TouchingAtBoundary()
        {
            var a = new RangeOf<double>(0, 10);
            var b = new RangeOf<double>(10, 20);

            Assert.True(a.Intersects(b));
            Assert.True(b.Intersects(a));
        }

        [Fact]
        public void Intersects_DisjointRanges()
        {
            var a = new RangeOf<double>(0, 9);
            var b = new RangeOf<double>(10, 20);

            Assert.False(a.Intersects(b));
            Assert.False(b.Intersects(a));
        }

        [Fact]
        public void Intersection_ReturnsNull_WhenNoIntersection()
        {
            var a = new RangeOf<double>(0, 9);
            var b = new RangeOf<double>(10, 20);

            var intersection = a.Intersection(b);
            Assert.Null(intersection);
        }

        [Fact]
        public void Intersection_ReturnsOverlapRange()
        {
            var a = new RangeOf<double>(0, 10);
            var b = new RangeOf<double>(5, 15);

            var intersection = a.Intersection(b);

            Assert.NotNull(intersection);
            Assert.Equal(5.0, intersection!.Start);
            Assert.Equal(10.0, intersection.End);
        }

        [Fact]
        public void Union_Throws_WhenDisjointAndNotAdjacent()
        {
            var a = new RangeOf<double>(0, 9);
            var b = new RangeOf<double>(10, 20);

            Assert.Throws<InvalidOperationException>(() => a.Union(b));
        }

        [Fact]
        public void Union_Works_WhenIntersecting()
        {
            var a = new RangeOf<double>(0, 10);
            var b = new RangeOf<double>(5, 15);

            var union = a.Union(b);

            Assert.Equal(0.0, union.Start);
            Assert.Equal(15.0, union.End);
        }

        [Fact]
        public void IsAdjacent_WithoutTolerance_TouchingAtBoundary()
        {
            var a = new RangeOf<double>(0, 10);
            var b = new RangeOf<double>(10, 20);

            Assert.True(a.IsAdjacent(b));
            Assert.True(b.IsAdjacent(a));
        }

        [Fact]
        public void IsAdjacent_WithTolerance_AllowsSmallGap()
        {
            var a = new RangeOf<double>(0, 10);
            var b = new RangeOf<double>(10.0001, 20);

            Assert.False(a.IsAdjacent(b, tolerance: 0.00001));
            Assert.True(a.IsAdjacent(b, tolerance: 0.001));
        }

        [Fact]
        public void Length_ReturnsNonNegativeSpan()
        {
            var range = new RangeOf<double>(2.0, 7.0);

            Assert.Equal(5.0, range.Length.Delta, 5);
        }

        [Fact]
        public void ClampLength_ClampsToMin()
        {
            var range = new RangeOf<double>(0.0, 10.0);
            var min = new RangeLength<double>(3.0);
            var max = new RangeLength<double>(8.0);

            var clamped = range.ClampLength(min, max);

            // Original length = 10, max = 8 -> sollte auf 8 gekürzt werden
            Assert.Equal(0.0, clamped.Start);
            Assert.Equal(8.0, clamped.End);
            Assert.Equal(8.0, clamped.Length.Delta, 5);
        }

        [Fact]
        public void ClampLength_ClampsToMax_WhenRangeTooShort()
        {
            var range = new RangeOf<double>(0.0, 2.0);
            var min = new RangeLength<double>(3.0);
            var max = new RangeLength<double>(5.0);

            var clamped = range.ClampLength(min, max);

            // Original length = 2, min = 3 -> sollte auf 3 verlängert werden
            Assert.Equal(0.0, clamped.Start);
            Assert.Equal(3.0, clamped.End);
            Assert.Equal(3.0, clamped.Length.Delta, 5);
        }

        [Fact]
        public void RangeMinusRange_ReturnsRangeLengthFromStartDifference()
        {
            var a = new RangeOf<double>(0.0, 5.0);
            var b = new RangeOf<double>(10.0, 15.0);

            var len = a - b;

            // In deinem Code: Difference(b.Start, a.Start) = 0 - 10 = -10 (je nach Math),
            // aber mit TestRangeMathDouble: end - start => 0 - 10 = -10
            // Hier prüfen wir nur, dass die signierte Differenz korrekt durchgereicht wird:
            Assert.Equal(-10.0, len.Delta, 5);
        }

        [Fact]
        public void ToString_ShowsStartAndEnd()
        {
            var range = new RangeOf<double>(1.0, 5.0);
            var text = range.ToString();

            Assert.Equal("[1 - 5]", text);
        }

    }
}
