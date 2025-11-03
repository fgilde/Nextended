using System;
using System.Threading.Tasks;
using Nextended.Core.Measurement;
using Xunit;

namespace Nextended.Core.Tests;

public class MeasureTests
{
    // --- Hilfs-Workloads -------------------------------------------------

    private static int SyncWorkloadAllocating()
    {
        // bisschen Arbeit + Allokationen
        var arr = new byte[1024 * 50]; // 50 KB
        for (int i = 0; i < arr.Length; i += 4096) arr[i] = (byte)(i % 256);
        return arr[0] + arr[^1];
    }

    private static async Task<int> AsyncWorkloadAllocating()
    {
        await Task.Delay(50).ConfigureAwait(false);
        var arr = new byte[1024 * 100]; // 100 KB
        for (int i = 0; i < arr.Length; i += 4096) arr[i] = (byte)(i % 256);
        return arr[0] + arr[^1];
    }

    // --- Sync ------------------------------------------------------------

    [Fact]
    public void Run_Sync_ReturnsResultAndTimingAndAllocations_ProcessMetric()
    {
        var r = Measure.Run(
            func: SyncWorkloadAllocating,
            memoryMetric: MemoryMetric.ProcessAllocatedBytes,
            precise: true
        );

        Assert.InRange(r.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        Assert.True(r.AllocatedBytes >= 0);
        Assert.Equal(SyncWorkloadAllocating().GetType(), r.Result.GetType());
    }

    [Fact]
    public void Run_Sync_ThreadLocalAllocatedBytes_YieldsPositiveAllocations()
    {
        var r = Measure.Run(
            func: SyncWorkloadAllocating,
            memoryMetric: MemoryMetric.ThreadAllocatedBytes
        );

        Assert.True(r.AllocatedBytes > 0);
    }

    [Fact]
    public void Run_Sync_NoneMemoryMetric_YieldsZeroAllocations()
    {
        var r = Measure.Run(
            func: SyncWorkloadAllocating,
            memoryMetric: MemoryMetric.None
        );

        Assert.Equal(0, r.AllocatedBytes);
    }

    [Fact]
    public void Run_Sync_NullFunc_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Measure.Run<int>(null!)
        );
    }

    // --- Async (Func<Task<T>>) ------------------------------------------

    [Fact]
    public async Task RunAsync_Async_ReturnsResultAndTimingAndAllocations_ProcessMetric()
    {
        var r = await Measure.RunAsync(
            func: AsyncWorkloadAllocating,
            memoryMetric: MemoryMetric.ProcessAllocatedBytes,
            precise: true
        );

        Assert.InRange(r.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        Assert.True(r.AllocatedBytes >= 0);
        Assert.Equal(await AsyncWorkloadAllocating(), r.Result);
    }

    [Fact]
    public async Task RunAsync_Async_NoneMemoryMetric_YieldsZeroAllocations()
    {
        var r = await Measure.RunAsync(
            func: AsyncWorkloadAllocating,
            memoryMetric: MemoryMetric.None
        );

        Assert.Equal(0, r.AllocatedBytes);
    }

    [Fact]
    public async Task RunAsync_Async_NullFunc_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await Measure.RunAsync<int>(null!)
        );
    }

    // --- Task<T> Extension ----------------------------------------------

    [Fact]
    public async Task MeasureAwait_TaskExtension_MeasuresAwaitedPart()
    {
        // Hinweis: Misst ab Await-Zeitpunkt – wenn Task vorher startete, ist das nicht enthalten.
        var runningTask = AsyncWorkloadAllocating(); // Task startet hier evtl. schon

        var r = await runningTask.ToMeasureResult(
            memoryMetric: MemoryMetric.ProcessAllocatedBytes,
            precise: true
        );

        Assert.InRange(r.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        Assert.True(r.AllocatedBytes >= 0);
        Assert.Equal(await AsyncWorkloadAllocating(), r.Result);
    }

    [Fact]
    public async Task MeasureAwait_NullTask_Throws()
    {
        Task<int> t = null!;
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await t.ToMeasureResult()
        );
    }

    // --- Smoke-Test ToString --------------------------------------------

    [Fact]
    public void MeasureResult_ToString_ContainsKeyParts()
    {
        var mr = new MeasureResult<int>(42, TimeSpan.FromMilliseconds(12), 12345);
        var s = mr.ToString();
        Assert.Contains("Elapsed=", s);
        Assert.Contains("Alloc=", s);
        Assert.Contains("Result=42", s);
    }
}