#if !NETSTANDARD2_0
using System.Collections;
using System.Runtime.InteropServices;

namespace Nextended.Core.COM
{
    /// <summary>
    /// IComList
    /// </summary>
    [ComVisible(true)]
    [Guid("3CD7F361-1D62-4BCC-B6C7-2AB7E199B624")]
    [TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FDispatchable)]
    public interface IComList : IEnumerable
    {
        /// <summary>
        /// Element hinzufügen
        /// </summary>
        /// <param name="aValue"></param>
        void Add([MarshalAs(UnmanagedType.IUnknown), In] object aValue);

        /// <summary>
        /// Element aus der Liste holen
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Get([In] int index);

        /// <summary>
        /// Anzahl von Elementen der Liste zurückgeben
        /// </summary>
        /// <returns></returns>
        int Count();
    }
}
#endif