using System.Collections.Generic;

namespace Nextended.Core.Facets;

public class AppliedFacet
{
    /// <summary>
    /// GroupKey for the filter group (e.g. "status").
    /// </summary>
    public string GroupKey { get; set; } = default!;

    /// <summary>
    /// Label for the chip (e.g. "Booked").
    /// </summary>
    public string Label { get; set; } = default!;

    /// <summary>
    /// Values for the chip (e.g. ["Booked", "InProgress"]).
    /// </summary>
    public List<string> Values { get; set; } = new();

    /// <summary>
    /// OData-Fragment for this specific filter (e.g. "status eq 'Booked' or status eq 'InProgress'").
    /// </summary>
    public string OData { get; set; } = default!;
}