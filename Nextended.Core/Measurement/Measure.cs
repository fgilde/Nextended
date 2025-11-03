#if !NETSTANDARD
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Nextended.Core.Measurement;

public static class Measure
{
    public static MeasureResult<T> Run<T>(
        Func<T> func,
        MemoryMetric memoryMetric = MemoryMetric.ProcessAllocatedBytes,
        bool precise = true // nur für ProcessAllocatedBytes relevant
    )
    {
        if (func is null) throw new ArgumentNullException(nameof(func));

        long before = ReadAllocated(memoryMetric, precise);
        var sw = Stopwatch.StartNew();

        var result = func();

        sw.Stop();
        long after = ReadAllocated(memoryMetric, precise);

        return new MeasureResult<T>(result, sw.Elapsed, SafeDelta(after, before));
    }

    public static async Task<MeasureResult<T>> RunAsync<T>(
        Func<Task<T>> func,
        MemoryMetric memoryMetric = MemoryMetric.ProcessAllocatedBytes,
        bool precise = true
    )
    {
        if (func is null) throw new ArgumentNullException(nameof(func));

        long before = ReadAllocated(memoryMetric, precise);
        var sw = Stopwatch.StartNew();

        var result = await func().ConfigureAwait(false);

        sw.Stop();
        long after = ReadAllocated(memoryMetric, precise);

        return new MeasureResult<T>(result, sw.Elapsed, SafeDelta(after, before));
    }


    public static async Task<MeasureResult<T>> ToMeasureResult<T>(
        this Task<T> task,
        MemoryMetric memoryMetric = MemoryMetric.ProcessAllocatedBytes,
        bool precise = true
    )
    {
        if (task is null) throw new ArgumentNullException(nameof(task));

        long before = ReadAllocated(memoryMetric, precise);
        var sw = Stopwatch.StartNew();

        var result = await task.ConfigureAwait(false);

        sw.Stop();
        long after = ReadAllocated(memoryMetric, precise);

        return new MeasureResult<T>(result, sw.Elapsed, SafeDelta(after, before));
    }

    private static long ReadAllocated(MemoryMetric metric, bool precise)
        => metric switch
        {
            MemoryMetric.None => 0L,
            MemoryMetric.ProcessAllocatedBytes => GC.GetTotalAllocatedBytes(precise),
            MemoryMetric.ThreadAllocatedBytes => GC.GetAllocatedBytesForCurrentThread(),
            _ => 0L
        };

    private static long SafeDelta(long after, long before)
        => after >= before ? after - before : 0L; // Schutz bei Overflow/Reset (theoretisch)
}
#endif