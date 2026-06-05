namespace Nextended.Core.Tests.classes
{
    /// <summary>
    /// Zelleninformationen
    /// </summary>
    public class CellObject 
    {
        /// <summary>
        /// Zellentyp
        /// </summary>
        public CellType CellType { get; set; }


        /// <summary>
        /// Ist die Zelle beschreibbar oder nicht?
        /// </summary>
        public bool IsEditable { get; set; }

        /// <summary>
        /// Zellenwert
        /// </summary>
        public CellValue Value { get; set; }

        /// <summary>
        /// Zellkommentare oder null
        /// </summary>
        public CellComment[] CellComments { get; set; }
    }
}