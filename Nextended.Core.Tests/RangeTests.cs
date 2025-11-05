using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Types;
using Nextended.Core.Contracts;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class RangeTests
	{
		[TestMethod]
		public void RangeOf_Constructor_WithStartAndEnd_CreatesRange()
		{
			var range = new RangeOf<int>(1, 10);
			
			Assert.AreEqual(1, range.Start);
			Assert.AreEqual(10, range.End);
		}

		[TestMethod]
		public void RangeOf_Constructor_WithSingleValue_CreatesRangeWithSameStartAndEnd()
		{
			var range = new RangeOf<int>(5);
			
			Assert.AreEqual(5, range.Start);
			Assert.AreEqual(5, range.End);
		}

		[TestMethod]
		public void RangeOf_Constructor_StartGreaterThanEnd_ThrowsException()
		{
			ExceptionAssert.Throws<ArgumentException>(
				() => new RangeOf<int>(10, 1),
				ex => ex.Message.Contains("Start value isn't less than or equal to the end value"));
		}

		[TestMethod]
		public void RangeOf_Contains_ValueInRange_ReturnsTrue()
		{
			var range = new RangeOf<int>(1, 10);
			
			Assert.IsTrue(range.Contains(5));
		}

		[TestMethod]
		public void RangeOf_Contains_ValueAtStart_ReturnsTrue()
		{
			var range = new RangeOf<int>(1, 10);
			
			Assert.IsTrue(range.Contains(1));
		}

		[TestMethod]
		public void RangeOf_Contains_ValueAtEnd_ReturnsTrue()
		{
			var range = new RangeOf<int>(1, 10);
			
			Assert.IsTrue(range.Contains(10));
		}

		[TestMethod]
		public void RangeOf_Contains_ValueOutsideRange_ReturnsFalse()
		{
			var range = new RangeOf<int>(1, 10);
			
			Assert.IsFalse(range.Contains(15));
			Assert.IsFalse(range.Contains(0));
		}

		[TestMethod]
		public void RangeOf_IsInRange_WorksSameAsContains()
		{
			var range = new RangeOf<int>(1, 10);
			
			Assert.IsTrue(range.IsInRange(5));
			Assert.IsFalse(range.IsInRange(15));
		}

		[TestMethod]
		public void RangeOf_Intersects_OverlappingRanges_ReturnsTrue()
		{
			var range1 = new RangeOf<int>(1, 10);
			var range2 = new RangeOf<int>(5, 15);
			
			Assert.IsTrue(range1.Intersects(range2));
			Assert.IsTrue(range2.Intersects(range1));
		}

		[TestMethod]
		public void RangeOf_Intersects_NonOverlappingRanges_ReturnsFalse()
		{
			var range1 = new RangeOf<int>(1, 10);
			var range2 = new RangeOf<int>(15, 20);
			
			Assert.IsFalse(range1.Intersects(range2));
			Assert.IsFalse(range2.Intersects(range1));
		}

		[TestMethod]
		public void RangeOf_Intersects_TouchingRanges_ReturnsTrue()
		{
			var range1 = new RangeOf<int>(1, 10);
			var range2 = new RangeOf<int>(10, 20);
			
			Assert.IsTrue(range1.Intersects(range2));
		}

		[TestMethod]
		public void RangeOf_Intersects_Null_ThrowsException()
		{
			var range = new RangeOf<int>(1, 10);
			
			ExceptionAssert.Throws<ArgumentNullException>(
				() => range.Intersects(null),
				ex => ex.ParamName == "other");
		}

		[TestMethod]
		public void RangeOf_Intersection_OverlappingRanges_ReturnsCorrectIntersection()
		{
			var range1 = new RangeOf<int>(1, 10);
			var range2 = new RangeOf<int>(5, 15);
			
			var result = range1.Intersection(range2);
			
			Assert.IsNotNull(result);
			Assert.AreEqual(5, result.Start);
			Assert.AreEqual(10, result.End);
		}

		[TestMethod]
		public void RangeOf_Intersection_NonOverlappingRanges_ReturnsNull()
		{
			var range1 = new RangeOf<int>(1, 10);
			var range2 = new RangeOf<int>(15, 20);
			
			var result = range1.Intersection(range2);
			
			Assert.IsNull(result);
		}

		[TestMethod]
		public void RangeOf_Union_OverlappingRanges_ReturnsCorrectUnion()
		{
			var range1 = new RangeOf<int>(1, 10);
			var range2 = new RangeOf<int>(5, 15);
			
			var result = range1.Union(range2);
			
			Assert.AreEqual(1, result.Start);
			Assert.AreEqual(15, result.End);
		}

		[TestMethod]
		public void RangeOf_Union_NonOverlappingNonAdjacentRanges_ThrowsException()
		{
			var range1 = new RangeOf<int>(1, 10);
			var range2 = new RangeOf<int>(15, 20);
			
			ExceptionAssert.Throws<ArgumentException>(
				() => range1.Union(range2),
				ex => ex.Message.Contains("Ranges do not overlap and are not adjacent"));
		}

		[TestMethod]
		public void RangeOf_Union_Null_ThrowsException()
		{
			var range = new RangeOf<int>(1, 10);
			
			ExceptionAssert.Throws<ArgumentNullException>(
				() => range.Union(null),
				ex => ex.ParamName == "other");
		}

		[TestMethod]
		public void RangeOf_AreAdjacent_WithoutFunction_ThrowsException()
		{
			var range1 = new RangeOf<int>(1, 10);
			var range2 = new RangeOf<int>(11, 20);
			
			ExceptionAssert.Throws<InvalidOperationException>(
				() => range1.AreAdjacent(range1, range2),
				ex => ex.Message.Contains("No function to check adjacency"));
		}

		[TestMethod]
		public void RangeOf_AreAdjacent_WithFunction_Works()
		{
			Func<IRange<int>, IRange<int>, bool> areAdjacent = (first, second) =>
			{
				return first.End + 1 == second.Start || second.End + 1 == first.Start;
			};
			
			var range1 = new RangeOf<int>(1, 10, areAdjacent);
			var range2 = new RangeOf<int>(11, 20);
			
			Assert.IsTrue(range1.AreAdjacent(range1, range2));
		}

		[TestMethod]
		public void RangeOf_ToString_ReturnsFormattedString()
		{
			var range = new RangeOf<int>(1, 10);
			var result = range.ToString();
			
			Assert.AreEqual("[1 - 10]", result);
		}

		[TestMethod]
		public void RangeOf_WithDateTime_Works()
		{
			var start = new DateTime(2024, 1, 1);
			var end = new DateTime(2024, 12, 31);
			var range = new RangeOf<DateTime>(start, end);
			
			var midYear = new DateTime(2024, 6, 15);
			Assert.IsTrue(range.Contains(midYear));
			
			var nextYear = new DateTime(2025, 1, 1);
			Assert.IsFalse(range.Contains(nextYear));
		}

		[TestMethod]
		public void RangeOf_WithString_Works()
		{
			var range = new RangeOf<string>("a", "z");
			
			Assert.IsTrue(range.Contains("m"));
			Assert.IsTrue(range.Contains("a"));
			Assert.IsTrue(range.Contains("z"));
		}

		[TestMethod]
		public void RangeOf_Union_WithAdjacentFunction_Works()
		{
			Func<IRange<int>, IRange<int>, bool> areAdjacent = (first, second) =>
			{
				return first.End + 1 == second.Start || second.End + 1 == first.Start;
			};
			
			var range1 = new RangeOf<int>(1, 10, areAdjacent);
			var range2 = new RangeOf<int>(11, 20);
			
			var result = range1.Union(range2);
			
			Assert.AreEqual(1, result.Start);
			Assert.AreEqual(20, result.End);
		}
	}
}
