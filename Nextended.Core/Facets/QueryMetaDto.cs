namespace Nextended.Core.Facets;

/// <summary>
/// Data transfer object containing OData query metadata such as sorting, pagination, and raw query information.
/// </summary>
public class QueryMetaDto
{
    /// <summary>
    /// Gets or sets the OData $orderby clause for sorting results.
    /// </summary>
    public string? OrderBy { get; set; }
    
    /// <summary>
    /// Gets or sets the OData $skip value for pagination (number of records to skip).
    /// </summary>
    public int? Skip { get; set; }
    
    /// <summary>
    /// Gets or sets the OData $top value for pagination (maximum number of records to return).
    /// </summary>
    public int? Top { get; set; }
    
    /// <summary>
    /// Gets or sets the raw OData query string as received from the client.
    /// </summary>
    public string? RawODataQuery { get; set; } 
}