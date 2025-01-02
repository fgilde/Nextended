#if !NETSTANDARD2_0
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Nextended.Core.COM
{
    /// <summary>
    /// Allgemeine Liste für COM Objekte
    /// </summary>
    [ComVisible(true)]
    [Guid("0D9E29E7-E40C-45F5-9996-4A9E0B45A614")]
    public class ComList : IComList
    {
        private readonly List<object> list;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComList" /> class.
        /// </summary>
        public ComList()
        {
            list = new List<object>();
        }

        #region Implementation of IComList

        /// <summary>
        /// Fügt Elemente in die Liste
        /// </summary>
        /// <param name="aValue">A value.</param>
        public void Add(object aValue)
        {
            list.Add(aValue);
        }

        /// <summary>
        /// Liefert das Element an der Stelle index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public object Get(int index)
        {
            return list[index];
        }

        /// <summary>
        /// Gibt die Anzahl der Elemente der Liste zurück
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return list.Count;
        }

        #endregion

        #region Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            return list.GetEnumerator();
        }

        #endregion
    }
}
#endif