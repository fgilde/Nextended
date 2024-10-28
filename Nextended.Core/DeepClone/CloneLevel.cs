namespace Nextended.Core.DeepClone
{
    /// <summary>
    /// CloneLevel
    /// FirstLevelOnly = Only InternalTypes
    /// Hierarki = All types Hierarki
    /// </summary>
    public enum CloneLevel
    {
        /// <summary>
        /// All types
        /// </summary>
        Hierarchical,
        /// <summary>
        /// Only InternalTypes
        /// </summary>
        FirstLevelOnly
    }
}
