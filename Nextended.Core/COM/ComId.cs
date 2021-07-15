using System;
using System.Runtime.InteropServices;

namespace Nextended.Core.COM
{
    /// <summary>
    /// COM Id für die Kommunikation zwischen CP-Server .NET und CP-Server Delphi. Wird im Delphi cpserverembedded.dll genutzt.
    /// </summary>
    [ComVisible(true)]
    [Guid("EF7BC4BE-110F-47FB-B1A5-6BB1C072ECFB")]
    public struct ComId
    {
        /// <summary>
        /// Int ID
        /// </summary>
        public int Int;

        /// <summary>
        /// Guid ID
        /// </summary>
        public Guid Guid;


        /// <summary>
        /// Setzt 
        /// </summary>        
        /// <param name="intvalue"></param>
        /// <param name="guid"></param>
        public ComId(int intvalue, Guid guid)
        {
            Int = intvalue;
            Guid = guid;
        }

        /// <summary>
        /// Setzt die uint ID in dem COM-Struct
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator ComId(uint value)
        {
            return new ComId { Guid = Guid.Empty, Int = (int)value };
        }

        /// <summary>
        /// Setzt die GUID ID in dem COM-Struct
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator ComId(Guid value)
        {
            return new ComId { Guid = value, Int = -1 };
        }

        /// <summary>
        /// Gibt die GUID ID aus dem COM-Struct
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator Guid(ComId value)
        {
            return value.Guid;
        }

        /// <summary>
        /// Gibt die INT ID aus dem COM-Struct
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator uint(ComId value)
        {
            return (uint)value.Int;
        }

        /// <summary>
        /// Gibt die INT ID aus dem COM-Struct
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator int(ComId value)
        {
            return value.Int;
        }
    }
}