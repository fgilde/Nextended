namespace Nextended.Core.Enums;

/// <summary>
/// Specifies how JSON arrays should be generated in code generation scenarios.
/// </summary>
public enum JsonArrayGeneration
{
    /// <summary>
    /// Generate as List&lt;T&gt; collection.
    /// </summary>
    List,
    
    /// <summary>
    /// Generate as T[] array.
    /// </summary>
    Array
}