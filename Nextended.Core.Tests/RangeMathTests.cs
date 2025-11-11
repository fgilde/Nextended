using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Contracts;
using Nextended.Core.Types;
using Nextended.Core.Types.Ranges.Math;

using System;
using System.Drawing;
using YamlDotNet.Core.Tokens;

namespace Nextended.Core.Tests
{
    [TestClass]
    public class RangeMathTests
    {
        // ---------- Kleine Test-Helfer (spiegeln Produktionslogik mit IRangeMath<T>) ----------

        private static double Percent<T>(T value, IRange<T> size) where T : IComparable<T> =>
            RangeMath<T>.Percent(value, size);

        private static T Lerp<T>(IRange<T> size, double pct) where T : IComparable<T> =>
            RangeMath<T>.Lerp(size, pct);

        private static T Clamp<T>(T v, IRange<T> bounds) where T : IComparable<T> => RangeMath<T>.Clamp(v, bounds);

        private static T SnapToStep<T>(T v, IRange<T> size, RangeLength<T> step, SnapPolicy policy = SnapPolicy.Nearest) where T : IComparable<T> =>
            RangeMath<T>.SnapToStep(v, size, step, policy);

        private static T AddSteps<T>(T v, RangeLength<T> step, int steps) where T : IComparable<T> => RangeMath<T>.AddSteps(v, step, steps);

        private static void AssertAreClose(double expected, double actual, double eps = 1e-9)
            => Assert.IsTrue(Math.Abs(expected - actual) <= eps, $"Expected {expected} but was {actual}");

        // ------------------------ INT ------------------------

        [TestMethod]
        public void Int_RangeLength_Operators_Work()
        {
            var l1 = new RangeLength<int>(5);
            var l2 = new RangeLength<int>(3);

            Assert.AreEqual(8, (l1 + l2).Delta);
            Assert.AreEqual(2, (l1 - l2).Delta);
            Assert.AreEqual(10, (l1 * 2).Delta);
            Assert.AreEqual(2.5, (l1 / 2).Delta);
            Assert.IsTrue(l1 == new RangeLength<int>(5));
            Assert.IsTrue(l1 != l2);
        }

        [TestMethod]
        public void Int_RangeOf_Length_And_Shift()
        {
            var r = new RangeOf<int>(10, 20);
            var len = r.Length; // 10
            Assert.AreEqual(10, len.Delta);

            var shifted = r + new RangeLength<int>(5);
            Assert.AreEqual(15, shifted.Start);
            Assert.AreEqual(25, shifted.End);

            var shiftedBack = shifted - new RangeLength<int>(5);
            Assert.AreEqual(10, shiftedBack.Start);
            Assert.AreEqual(20, shiftedBack.End);
        }

        [TestMethod]
        public void Int_RangeOf_Difference_And_Union()
        {
            var a = new RangeOf<int>(0, 10);
            var b = new RangeOf<int>(5, 15);

            // Distanz der Startpunkte
            var d = a - b;
            Assert.AreEqual(0 - 5, d.Delta);

            // Union (überlappend)
            var u = a + b;
            Assert.AreEqual(0, u.Start);
            Assert.AreEqual(15, u.End);
        }

        [TestMethod]
        public void Int_Math_Lerp_Percent_Snap_AddSteps()
        {
            var size = new RangeOf<int>(0, 100);
            var p50 = Percent(50, size);
            AssertAreClose(0.5, p50);

            var mid = Lerp(size, 0.5);
            Assert.AreEqual(50, mid);

            var step = new RangeLength<int>(5);
            Assert.AreEqual(50, SnapToStep(52, size, step));
            Assert.AreEqual(55, SnapToStep(54, size, step));

            Assert.AreEqual(70, AddSteps(50, step, 4));   // 50 + 4*5
            Assert.AreEqual(40, AddSteps(50, step, -2));  // 50 - 2*5
        }

        // ------------------------ DOUBLE ------------------------

        [TestMethod]
        public void Double_RangeLength_Operators_Work()
        {
            var l1 = new RangeLength<double>(2.5);
            var l2 = new RangeLength<double>(0.5);

            AssertAreClose(3.0, (l1 + l2).Delta);
            AssertAreClose(2.0, (l1 - l2).Delta);
            AssertAreClose(5.0, (l1 * 2).Delta);
            AssertAreClose(1.25, (l1 / 2).Delta);
        }

        [TestMethod]
        public void Double_RangeOf_Length_Shift_Union()
        {
            var r = new RangeOf<double>(1.25, 3.75);
            AssertAreClose(2.5, r.Length.Delta);

            var shifted = r + new RangeLength<double>(1.0);
            AssertAreClose(2.25, shifted.Start);
            AssertAreClose(4.75, shifted.End);

            var a = new RangeOf<double>(0.0, 1.0);
            var b = new RangeOf<double>(0.5, 2.0);
            var u = a + b;
            AssertAreClose(0.0, u.Start);
            AssertAreClose(2.0, u.End);
        }

        [TestMethod]
        public void Double_Math_Lerp_Percent_Snap_AddSteps()
        {
            var size = new RangeOf<double>(0.0, 10.0);
            AssertAreClose(0.25, Percent(2.5, size));

            var q3 = Lerp(size, 0.75);
            AssertAreClose(7.5, q3);

            var step = new RangeLength<double>(0.4);
            AssertAreClose(2.4, RangeMathFactory.For<double>().ToDouble(SnapToStep(2.37, size, step)), 1e-9);

            var m = RangeMathFactory.For<double>();
            var add = AddSteps(1.0, step, 3); // 1.0 + 3*0.4 = 2.2
            AssertAreClose(2.2, m.ToDouble(add));
        }

        // ------------------------ DECIMAL ------------------------

        [TestMethod]
        public void Decimal_Range_Length_And_Snap()
        {
            var r = new RangeOf<decimal>(10m, 10.9m);
            AssertAreClose(0.9, r.Length.Delta, 1e-9);

            var size = new RangeOf<decimal>(0m, 100m);
            var step = new RangeLength<decimal>(2.5);
            var snapped = SnapToStep(12.3m, size, step); // -> 12.5
            Assert.AreEqual(12.5m, snapped);
        }

        // ------------------------ DATETIME ------------------------

        [TestMethod]
        public void DateTime_Length_Shift_Lerp_Snap_AddSteps()
        {
            var start = new DateTime(2024, 1, 1);
            var end = new DateTime(2024, 1, 31);
            var r = new RangeOf<DateTime>(start, end);

            // Length (Ticks-Differenz)
            var len = r.Length;
            var expectedTicks = (end - start).Ticks;
            Assert.AreEqual(expectedTicks, (long)len.Delta);

            // Shift um 7 Tage
            var sevenDays = new RangeLength<DateTime>(TimeSpan.FromDays(7).Ticks);
            var r2 = r + sevenDays;
            Assert.AreEqual(start.AddDays(7), r2.Start);
            Assert.AreEqual(end.AddDays(7), r2.End);

            // Lerp Mitte
            var mid = Lerp(r, 0.5);
            Assert.AreEqual(start + TimeSpan.FromTicks(expectedTicks / 2), mid);

            // Snap auf 1 Tag
            var size = new RangeOf<DateTime>(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
            var oneDay = new RangeLength<DateTime>(TimeSpan.FromDays(1).Ticks);
            var any = new DateTime(2024, 3, 5, 15, 23, 0);
            var snapped = SnapToStep(any, size, oneDay);
            Assert.AreEqual(new DateTime(2024, 3, 6, 0, 0, 0), snapped); // auf den nächsten Tag gerundet

            // AddSteps
            var plus10 = AddSteps(start, oneDay, 10);
            Assert.AreEqual(start.AddDays(10), plus10);
        }

        // ------------------------ DATEONLY ------------------------

        [TestMethod]
        public void DateOnly_Length_Shift_Snap()
        {
            var s = new DateOnly(2024, 1, 1);
            var e = new DateOnly(2024, 1, 10);
            var r = new RangeOf<DateOnly>(s, e);

            // Length als DayNumber-Differenz
            Assert.AreEqual(9, r.Length.Delta);

            // Verschieben um 3 Tage
            var threeDays = new RangeLength<DateOnly>(3); // DayNumber-Einheiten
            var r2 = r + threeDays;
            Assert.AreEqual(new DateOnly(2024, 1, 4), r2.Start);
            Assert.AreEqual(new DateOnly(2024, 1, 13), r2.End);

            // Snap auf 7-Tage-Raster
            var year = new RangeOf<DateOnly>(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));
            var step = new RangeLength<DateOnly>(7);
            var any = new DateOnly(2024, 1, 10);
            var sn = SnapToStep(any, year, step); // nächster 7er Schritt ab 1.1.
            Assert.AreEqual(new DateOnly(2024, 1, 8), sn);
        }

        // ------------------------ TIMEONLY ------------------------

        [TestMethod]
        public void TimeOnly_Length_Shift_Snap()
        {
            var s = new TimeOnly(8, 0, 0);
            var e = new TimeOnly(12, 0, 0);
            var r = new RangeOf<TimeOnly>(s, e);

            // Length (Ticks)
            var lenTicks = (e.ToTimeSpan() - s.ToTimeSpan()).Ticks;
            Assert.AreEqual((double)lenTicks, r.Length.Delta, 0);

            // + 1 Stunde
            var oneHour = new RangeLength<TimeOnly>(TimeSpan.FromHours(1).Ticks);
            var shifted = r + oneHour;
            Assert.AreEqual(new TimeOnly(9, 0, 0), shifted.Start);
            Assert.AreEqual(new TimeOnly(13, 0, 0), shifted.End);

            // Snap auf 15 Minuten
            var day = new RangeOf<TimeOnly>(new TimeOnly(0, 0, 0), new TimeOnly(23, 59, 59));
            var q15 = new RangeLength<TimeOnly>(TimeSpan.FromMinutes(15).Ticks);
            var t = new TimeOnly(10, 7, 0);
            var sn = SnapToStep(t, day, q15, SnapPolicy.Ceiling);
            Assert.AreEqual(new TimeOnly(10, 15, 0), sn);
        }

        // ------------------------ TIMESPAN ------------------------

        [TestMethod]
        public void TimeSpan_Length_Shift_Snap()
        {
            var s = TimeSpan.FromHours(1);
            var e = TimeSpan.FromHours(3);
            var r = new RangeOf<TimeSpan>(s, e);

            Assert.AreEqual(TimeSpan.FromHours(2).Ticks, (long)r.Length.Delta);

            var thirty = new RangeLength<TimeSpan>(TimeSpan.FromMinutes(30).Ticks);
            var r2 = r + thirty;
            Assert.AreEqual(TimeSpan.FromHours(1.5), r2.Start);
            Assert.AreEqual(TimeSpan.FromHours(3.5), r2.End);

            var day = new RangeOf<TimeSpan>(TimeSpan.Zero, TimeSpan.FromDays(1));
            var snapped = SnapToStep(TimeSpan.FromHours(1.75), day, thirty, SnapPolicy.Ceiling); // -> 2.0
            Assert.AreEqual(TimeSpan.FromHours(2), snapped);
        }

        // ------------------------ RANGE GUARDS & CLAMP LENGTH ------------------------

        [TestMethod]
        public void RangeOf_ClampLength_Works_For_Int()
        {
            var r = new RangeOf<int>(10, 12); // Länge 2
            var minLen = new RangeLength<int>(5);
            var maxLen = new RangeLength<int>(8);

            var clamped = r.ClampLength(minLen, maxLen);
            Assert.AreEqual(10, clamped.Start);
            Assert.AreEqual(15, clamped.End); // auf min 5 verlängert
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void RangeOf_Union_Disjoint_Throws()
        {
            var a = new RangeOf<int>(0, 5);
            var b = new RangeOf<int>(10, 15);
            var _ = a + b; // disjoint → Exception (kein Adjacent-Union)
        }

        // Hinweis: Wenn du Adjacent-Union erlauben willst (z. B. [0..5] + [5..10] => [0..10]),
        // ergänze eine IsAdjacent-Logik in deinem RangeOf<T> und füge hier einen Test:
        // [TestMethod]
        // public void RangeOf_Union_Adjacent_Works_When_Enabled() { ... }
    }
}
