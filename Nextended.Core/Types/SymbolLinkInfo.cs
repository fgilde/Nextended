namespace Nextended.Core.Types
{
    /// <summary>
    /// Represents information about a symbolic link, including the link path and its target.
    /// </summary>
    public class SymbolLinkInfo
    {
        /// <summary>
        /// Gets the path to the symbolic link.
        /// </summary>
        public string LinkName { get; private set; }
        
        /// <summary>
        /// Gets the target path that the symbolic link points to.
        /// </summary>
        public string Target { get; private set; }

        /// <summary>
        /// Initializes a new instance of the SymbolLinkInfo class.
        /// </summary>
        /// <param name="linkName">The path to the symbolic link.</param>
        /// <param name="target">The target path that the link points to.</param>
        public SymbolLinkInfo(string linkName, string target)
        {
            LinkName = linkName;
            Target = target;
        }
    }
}