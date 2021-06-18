using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;

namespace Nextended.UI.WPF.Converters
{
    /// <summary>
    /// StaticMethodToValueConverter
    /// </summary>
    public class StaticMethodToValueConverter : IValueConverter
    {
        /// <summary>
        /// Konvertiert eine Statische Methode der übergebenen Klasse zu einem Wert, um extension methods für ein objekt zu binden
        /// <example>
        ///    Text="{Binding ConverterParameter=Namespace.MyStaticExtensions.GetVersion, Converter={StaticResource callMethodMethodConverter}}" />
        /// </example>
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null || String.IsNullOrEmpty(parameter.ToString()))
                return null;

            var strings = parameter.ToString().Split('.');
            if (strings.Count() < 2)
                return value;

            string methodName = strings.Last();
            string name = String.Empty;
            for (int i = 0; i < strings.Length-1; i++)
            {
                name += strings[i];
                if (i < strings.Length - 2)
                    name += ".";
            }

            var referencedAssemblies = value.GetType().Assembly.GetReferencedAssemblies().Where(assemblyName => name.Contains(assemblyName.Name));
            foreach (AssemblyName assemblyName in referencedAssemblies)
            {
                Assembly assembly = Assembly.Load(assemblyName);
                Type thisClass = assembly.GetType(name, false, true);
                if (thisClass != null)
                {
                    MethodInfo m = thisClass.GetMethod(methodName);
                    if (m != null)
                    {
                        var parameters = new object[m.GetParameters().Count()];
                        if(parameters.Length >=1)
                            parameters[0] = value;
                        return m.Invoke(null, parameters);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value that is produced by the binding target.</param><param name="targetType">The type to convert to.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
        	return value != null ? value.ToString() : null;
        }
    }
}