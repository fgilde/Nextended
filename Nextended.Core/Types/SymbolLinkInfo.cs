namespace Nextended.Core.Types
{
    public class SymbolLinkInfo
    {
        public string LinkName { get; private set; }
        public string Target { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public SymbolLinkInfo(string linkName, string target)
        {
            LinkName = linkName;
            Target = target;
        }
    }
}