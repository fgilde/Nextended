using Nextended.Core.Types;
using Nextended.Core.Types.Ranges.Math;
using System;
using Xunit;

namespace Nextended.Core.Tests;

public class RangeMathCompareTests
{
    private const double Epsilon = 1e-9;

    private static void CompareAll<T>(Func<Random, T> valueFactory, int iterations = 10_000)
    {
        var rng = new Random(12345);

        IRangeMath<T> universal = new UniversalRangeMath<T>();
        IRangeMath<T> numeric = new NumericRangeMath<T>();

        for (int i = 0; i < iterations; i++)
        {
            var start = valueFactory(rng);
            var end = valueFactory(rng);
            var value = valueFactory(rng);

            // delta im vernünftigen Bereich halten
            var delta = rng.NextDouble() * 2_000.0 - 1_000.0; // -1000 .. +1000

            // --- ToDouble ---
            var uToDoubleStart = universal.ToDouble(start);
            var nToDoubleStart = numeric.ToDouble(start);
            AssertAlmostEqual(nToDoubleStart, uToDoubleStart, $"ToDouble(start) mismatch for {typeof(T)}");

            var uToDoubleEnd = universal.ToDouble(end);
            var nToDoubleEnd = numeric.ToDouble(end);
            AssertAlmostEqual(nToDoubleEnd, uToDoubleEnd, $"ToDouble(end) mismatch for {typeof(T)}");

            // --- Difference ---
            var uDiff = universal.Difference(start, end);
            var nDiff = numeric.Difference(start, end);
            AssertAlmostEqual(nDiff, uDiff, $"Difference(start, end) mismatch for {typeof(T)}");

            // --- Add ---
            bool uAddThrew = false;
            bool nAddThrew = false;
            Exception? uEx = null;
            Exception? nEx = null;

            T? uAddResult = default;
            T? nAddResult = default;

            try
            {
                uAddResult = universal.Add(value, delta);
            }
            catch (Exception ex)
            {
                uAddThrew = true;
                uEx = ex;
            }

            try
            {
                nAddResult = numeric.Add(value, delta);
            }
            catch (Exception ex)
            {
                nAddThrew = true;
                nEx = ex;
            }

            // Wenn eine Methode wirft, sollten beide das gleiche Verhalten zeigen
            Assert.Equal(nAddThrew, uAddThrew);

            if (uAddThrew && nAddThrew)
            {
                // Optional: gleicher Exception-Typ
                Assert.Equal(nEx!.GetType(), uEx!.GetType());
            }
            else
            {
                // Beide erfolgreich => Ergebnis vergleichen über ToDouble
                var uAddDouble = universal.ToDouble(uAddResult!);
                var nAddDouble = numeric.ToDouble(nAddResult!);
                AssertAlmostEqual(nAddDouble, uAddDouble, $"Add(value, delta) mismatch for {typeof(T)}");
            }
        }
    }

    private static void AssertAlmostEqual(double expected, double actual, string? message = null)
    {
        var diff = Math.Abs(expected - actual);
        if (double.IsNaN(expected) && double.IsNaN(actual))
            return;
        if (double.IsInfinity(expected) || double.IsInfinity(actual))
        {
            Assert.Equal(expected, actual);
            return;
        }

        Assert.True(
            diff <= Epsilon,
            message ?? $"Expected {expected} ≈ {actual}, diff={diff} > {Epsilon}"
        );
    }

    // ---------------------------
    // Konkrete Tests pro Typ
    // ---------------------------

    [Fact]
    public void Int_Behaviour_Is_Equivalent()
    {
        // Bereich begrenzen, um Überläufe beim Add zu vermeiden
        CompareAll<int>(rng => rng.Next(int.MinValue / 4, int.MaxValue / 4));
    }

    [Fact]
    public void Long_Behaviour_Is_Equivalent()
    {
        // long.Next() gibt es nicht direkt, also zusammensetzen
        CompareAll<long>(rng =>
        {
            // 32-Bit random in long "hochziehen"
            var high = (long)rng.Next(int.MinValue, int.MaxValue);
            var low = (long)rng.Next(int.MinValue, int.MaxValue);
            return (high << 32) ^ low;
        });
    }

    [Fact]
    public void Float_Behaviour_Is_Equivalent()
    {
        CompareAll<float>(rng =>
        {
            // float im begrenzten Bereich
            var v = (float)(rng.NextDouble() * 2_000_000.0 - 1_000_000.0);
            return v;
        });
    }

    [Fact]
    public void Double_Behaviour_Is_Equivalent()
    {
        CompareAll<double>(rng =>
        {
            var v = rng.NextDouble() * 2_000_000.0 - 1_000_000.0;
            return v;
        });
    }

    [Fact]
    public void Decimal_Behaviour_Is_Equivalent()
    {
        CompareAll<decimal>(rng =>
        {
            // Decimal aus Double generieren, aber Werte etwas im Rahmen halten
            var v = (decimal)(rng.NextDouble() * 2_000_000.0 - 1_000_000.0);
            return v;
        });
    }
}
