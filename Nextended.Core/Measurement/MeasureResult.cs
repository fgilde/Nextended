#if !NETSTANDARD
using System;

namespace Nextended.Core.Measurement;

public enum MemoryMetric
{
    None,
    ProcessAllocatedBytes,  
    ThreadAllocatedBytes   
}

public readonly record struct MeasureResult<T>(
    T Result,
    TimeSpan Elapsed,
    long AllocatedBytes
)
{
    public override string ToString()
        => $"Elapsed={Elapsed}, Alloc={AllocatedBytes:N0} B, Result={Result}";
}

#endif