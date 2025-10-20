namespace Nextended.Core.Facets;

public class QueryMetaDto
{
    public string? OrderBy { get; set; }
    public int? Skip { get; set; }
    public int? Top { get; set; }
    public string? RawODataQuery { get; set; } 
}