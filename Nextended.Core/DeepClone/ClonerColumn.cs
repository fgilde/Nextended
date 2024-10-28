using System;

namespace Nextended.Core.DeepClone
{
    /// <summary>
    /// This attribute is used be CloneTo Method
    /// Add this attribute to map property from one class to another class
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ClonerColumn : Attribute
    {
        /// <summary>
        /// ColumnName
        /// </summary>
        public string ColumnName { get; }
        /// <summary>
        /// map property from one class to another class
        /// </summary>
        /// <param name="columnName"></param>
        public ClonerColumn(string columnName)
        {
            ColumnName = columnName;
        }
    }
}
