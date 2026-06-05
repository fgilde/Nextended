namespace Nextended.Core.Tests.classes
{
    /// <summary>
    ///     Zellenwert
    /// </summary>
    public class CellValue
    {
    }

    /// <summary>
    ///     Zahlenwert
    /// </summary>
    public class DoubleValue : CellValue
    {
        /// <summary>
        ///     Zellenwert
        /// </summary>
        public double? Value { get; set; }
    }

    /// <summary>
    ///     Zahlenwert mit Warnfarbe
    /// </summary>
    public class DoubleValueWarnlight : DoubleValue
    {
        /// <summary>
        /// Warnfarbe
        /// </summary>
        public WarnlightType Warnlight { get; set; }
    }

    /// <summary>
    /// Text
    /// </summary>
    public class TextValue : CellValue
    {
        /// <summary>
        /// Text
        /// </summary>
        public string Value { get; set; }   
    }

    /// <summary>
    /// Integerwert
    /// </summary>
    public class IntegerValue : CellValue
    {
        /// <summary>
        /// Integer-Wert
        /// </summary>
        public int Value { get; set; }  
    }
}