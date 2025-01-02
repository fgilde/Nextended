using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nextended.Core.Extensions;
using Nextended.Core.TypeConverters;

namespace Nextended.Core.Helper
{

    /// <summary>
    /// Settings für das Classmapping
    /// </summary>
    public class ClassMappingSettings
    {
        private static ClassMappingSettings defaults;
        private static readonly Dictionary<Assembly, List<TypeConverter>> assembliesTypeConverters = new Dictionary<Assembly, List<TypeConverter>>();
        internal static ConcurrentBag<TypeConverter> GlobalConverters = new ConcurrentBag<TypeConverter>();
        internal readonly List<MemberInfo> PropertiesToIgnore = new List<MemberInfo>();
        internal readonly List<KeyValuePair<MemberInfo, MemberInfo>> PropertiesToAssign = new List<KeyValuePair<MemberInfo, MemberInfo>>();
        internal static ClassMappingSettings CurrentSettings { get; private set; }

        #region Global Static functions

        /// <summary>
        /// Clears the global converters.
        /// </summary>
        public static void ClearGlobalConverters()
        {
            GlobalConverters = new ConcurrentBag<TypeConverter>();
        }

        /// <summary>
        /// Typeconverter zu den einstellungen hinzufügen
        /// </summary>
        public static ClassMappingSettings RemoveGlobalConverter(TypeConverter converter)
        {
            if (converter != null && GlobalConverters.Contains(converter))
            {
                var list = new List<TypeConverter>(GlobalConverters.ToArray());
                if (list.Contains(converter))
                {
                    list.Remove(converter);
                    GlobalConverters = new ConcurrentBag<TypeConverter>(list);
                }
            }
            return (CurrentSettings ?? Default).RemoveConverter(converter);
        }

        /// <summary>
        /// Globalen Konverter hinzufügen der dann immer bei allen Settings greift, es sei denn die Setting hat 
        /// </summary>
        public static void AddGlobalConverters(params TypeConverter[] converters)
        {
            foreach (var converter in converters)
                GlobalConverters.Add(converter);
        }

        /// <summary>
        /// Eine Func als Type Converter hinzufügen
        /// </summary>
        public static TypeConverter AddGlobalConverter<TIn, TOut>(Func<TIn, TOut> fn = null, bool allowAssignableInputs = false)
        {
            GenericTypeConverter<TIn, TOut> converter = new GenericTypeConverter<TIn, TOut>(null, allowAssignableInputs);
            converter.SetConverterFunc(fn ?? (m => m.MapTo<TOut>()));
            AddGlobalConverters(converter);
            return converter;
        }

        /// <summary>
        /// Eine Func als Type Converter hinzufügen
        /// </summary>
        public static TypeConverter AddGlobalConverter(Type tIn, Type tOut, Func<object, object> fn = null,
            bool allowAssignableInputs = false)
        {
            SimpleFuncConverter converter = new SimpleFuncConverter(tIn, tOut, null, allowAssignableInputs);
            converter.SetConverterFunc(fn ?? (m => m.MapTo(tOut)));
            AddGlobalConverters(converter);
            return converter;
        }

        #endregion


        /// <summary>
        /// Standard Settings
        /// </summary>
        public static ClassMappingSettings Default => defaults ?? new ClassMappingSettings();

        /// <summary>
		/// Settings für schnelles Mapping
		/// </summary>
		public static ClassMappingSettings Fast
        {
            get
            {
                return new ClassMappingSettings(true)
                    .Set(s => s.IgnoreExceptions = true,
                         s => s.AutoCheckForDataContractJsonSerializer = false,
                         s => s.ShouldEnumeratePropertiesAsync = true,
                         s => s.TryContainerResolve = false,
                         s => s.SearchForTryParseInTargetTypes = false
                         );
            }
        }

        public ClassMappingSettings SetAsDefault()
        {
            defaults = this;
            return this;
        }

        /// <summary>
        /// Die hier angegebenen Properties werden beim Mapping ignoriert
        /// </summary>
        public ClassMappingSettings IgnoreProperties<TInput>(params Expression<Func<TInput, object>>[] toIgnore)
        {
            toIgnore.Apply(expression => PropertiesToIgnore.Add(expression.GetMemberInfo()));
            return this;
        }

        /// <summary>
        /// Die hier angegebenen Properties werden beim Mapping ignoriert
        /// </summary>
        public ClassMappingSettings IgnoreProperties(params MemberInfo[] toIgnore)
        {
            PropertiesToIgnore.AddRange(toIgnore);
            return this;
        }


        /// <summary>
        /// Gibt an ob assignments vorhanden sind
        /// </summary>
        public bool HasAssignments { get; private set; }

        /// <summary>
        /// Eine liste muss min so viele einträge haben wie <see cref="MinListCountToEnumerateAsync"/> damit diese asynchron enumeriert wird
        /// </summary>
        public int MinListCountToEnumerateAsync { get; set; }

        /// <summary>
        /// Gibt an ob zum erzeugen eines Typs versucht werden soll diesen mit Unity zu resolven (schneller wenn nicht)
        /// </summary>
        public bool TryContainerResolve { get; set; }

        /// <summary>
        /// Wenn diese Option auf true steht werden die Global Konverter nicht berücksichtigt
        /// </summary>
        public bool IgnoreGlobalConverters { get; set; }

        /// <summary>
        /// Wenn diese Option true ist und der aktuelle wert einem default wert des 
        /// valuetypes entspricht wird wenn der target type kein ValueType ist immer null zurückgeben, 
        /// ansonsten wird der result type erzeugt und wenn möglich konvetiert oder zugewiesen.
        /// </summary>
        public bool DefaultValueTypeValuesAsNullForNonValueTypes { get; set; }

        /// <summary>
        /// Bei true, können int,long und string immer zu oder von Guids konvetiert werden
        /// </summary>
        public bool AllowGuidConversion { get; set; }

        /// <summary>
        /// Wenn diese Option true ist muss bei string to enum Groß und kleinschreibung stimmen
        /// </summary>
        public bool MatchCaseForEnumNameConversion { get; set; }

        /// <summary>
        ///  Wenn diese Option true ist wird automatisch beim target type nach einer methode TryParse gesucht und ggf zum Konvertieren benutzt
        /// </summary>
        public bool SearchForTryParseInTargetTypes { get; set; }

        /// <summary>
        /// Gibt an, ob exceptions weitergeworfen werden sollen oder nicht
        /// </summary>
        public bool IgnoreExceptions { get; set; }

        /// <summary>
        /// Wenn true, dann werden auch private Member gemapped. 
        /// (wenn eine Klasse z.B für NotifyPropertyChanged viele backing fields hat, sollte diese option auf false (default) stehen)
        /// </summary>
        public bool IncludePrivateFields { get; set; }

        /// <summary>
        /// Wenn true werden Abstrakte basis properties überdeckt
        /// </summary>
        public bool CoverUpAbstractMembers { get; set; }

        /// <summary>
        /// Liste der Konverter die für das Mapping berücksichtigt werden sollen
        /// </summary>
        internal ConcurrentBag<TypeConverter> TypeConverters { get; private set; }

        /// <summary>
        /// Wenn auf true, werden listen asyncron befüllt, dieses ist zwar schneller, 
        /// doch ist dann die Reihenfolge der liste ggf nicht die gleiche wie bei dem input objekt
        /// </summary>
        public bool ShouldEnumerateListsAsync { get; set; }

        /// <summary>
        /// Wenn diese Option true ist wird ein Objekt per mapTo string zu einem JSON String
        /// </summary>
        public bool ObjectToStringWithJSON { get; set; }

        /// <summary>
        /// Wenn diese Option an kann man aus einem JSON string per mapto das entsprechende Opjekt desirialisieren
        /// </summary>
        public bool CanConvertFromJSON { get; set; }

        /// <summary>
        /// Wenn diese Option an ist wird geprüft ob bei der JSON Konvertierung ggf der DataContractJsonSerializer benutzt werden kann (dauert etwas länger)
        /// </summary>
        public bool AutoCheckForDataContractJsonSerializer { get; set; }

        /// <summary>
        /// Gibt an ob Properties asncron befüllt werden sollen
        /// </summary>
        public bool ShouldEnumeratePropertiesAsync { get; set; }

        /// <summary>
        /// ServiceProvider wird benutzt wenn <see cref="TryContainerResolve"/> auf true steht um das erste resolve des target typen / interface zu machen
        /// </summary>
        public IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public ClassMappingSettings(bool shouldEnumerateListsAsync = false,
            params TypeConverter[] specificConverters)
        {
            CanConvertFromJSON = true;
            AutoCheckForDataContractJsonSerializer = true;

            CurrentSettings = this;
            AllowGuidConversion = true;
            MinListCountToEnumerateAsync = 100;
            SearchForTryParseInTargetTypes = true;
            ShouldEnumerateListsAsync = shouldEnumerateListsAsync;
            TypeConverters = new ConcurrentBag<TypeConverter>(specificConverters);
            ShouldEnumeratePropertiesAsync = !System.Diagnostics.Debugger.IsAttached;
            CoverUpAbstractMembers = false;
            IncludePrivateFields = false;
            MatchCaseForEnumNameConversion = false;
            AddDefaultConverters();
        }

        private void AddDefaultConverters()
        {
#if !NETSTANDARD

            AddConverter<DateOnly, DateTime>(only => only.ToDateTime(new TimeOnly(0, 0)));
            AddConverter<DateTime, DateOnly>(DateOnly.FromDateTime);

            AddConverter<TimeOnly, DateTime>(only => new DateTime(DateTime.MinValue.Year, DateTime.MinValue.Month, DateTime.MinValue.Day, only.Hour, only.Minute, only.Second));
            AddConverter<DateTime, TimeOnly>(TimeOnly.FromDateTime);

            AddConverter<TimeOnly, TimeSpan>(only => only.ToTimeSpan());
            AddConverter<TimeSpan, TimeOnly>(TimeOnly.FromTimeSpan);
#endif
        }

        public ClassMappingSettings RemoveConverters(params TypeConverter[] converters)
        {
            converters.Apply(c => RemoveConverter(c));
            return this;
        }

        /// <summary>
        /// Typeconverter zu den einstellungen hinzufügen
        /// </summary>
        public ClassMappingSettings RemoveConverter(TypeConverter converter)
        {
            if (converter != null && TypeConverters != null && TypeConverters.Contains(converter))
            {
                var list = new List<TypeConverter>(TypeConverters.ToArray());
                if (list.Contains(converter))
                {
                    list.Remove(converter);
                    TypeConverters = new ConcurrentBag<TypeConverter>(list);
                }
            }

            return this;
        }

        /// <summary>
        /// Einfaches type mapping hinzufügen bei dem dann automatisch wieder mapTo greift
        /// </summary>
        public ClassMappingSettings AddTypeMapping<TIn, TOut>(bool allowAssignableInputs = false)
        {
            return AddConverter<TIn, TOut>(null, allowAssignableInputs);
        }

        /// <summary>
        /// Property assignment hinzufügen z.B falls properties unterschiedliche Namen haben
        /// </summary>
        public ClassMappingSettings AddAssignment<TInput, TResult>(Expression<Func<TInput, object>> inProp, Expression<Func<TResult, object>> outProp)
        {
            return AddAssignment(inProp.GetMemberInfo(), outProp.GetMemberInfo());
        }

        /// <summary>
        /// Property assignment hinzufügen z.B falls properties unterschiedliche Namen haben
        /// </summary>
        public ClassMappingSettings AddAssignment<TResult>(MemberInfo inputProperty, Expression<Func<TResult, object>> outProp)
        {
            return AddAssignment(inputProperty, outProp.GetMemberInfo());
        }

        /// <summary>
        /// Property assignment hinzufügen z.B falls properties unterschiedliche Namen haben
        /// </summary>
        public ClassMappingSettings AddAssignment(MemberInfo inputProperty, MemberInfo outputProperty)
        {
            HasAssignments = true;
            PropertiesToAssign.Add(new KeyValuePair<MemberInfo, MemberInfo>(inputProperty, outputProperty));
            return IgnoreProperties(inputProperty);
        }

        /// <summary>
        /// Type converter zu den einstellungen hinzufügen
        /// </summary>
        public ClassMappingSettings AddConverters(params TypeConverter[] converters)
        {
            foreach (var converter in converters)
                TypeConverters.Add(converter);
            return this;
        }

        /// <summary>
        /// Typeconverter zu den einstellungen hinzufügen
        /// </summary>
        public ClassMappingSettings AddConverter(TypeConverter converter)
        {
            TypeConverters.Add(converter);
            return this;
        }

        /// <summary>
        /// Eine Func als Type Converter hinzufügen
        /// </summary>
        public ClassMappingSettings AddConverter<TIn, TOut>(Func<TIn, TOut> fn = null, bool allowAssignableInputs = false)
        {
            GenericTypeConverter<TIn, TOut> converter = new GenericTypeConverter<TIn, TOut>(null, allowAssignableInputs);
            converter.SetConverterFunc(fn ?? (m => m.MapTo<TOut>(RemoveConverter(converter)))); // Eigenen converter eintfernen (sonst droht stackoverflow)
            return AddConverter(converter);
        }

        /// <summary>
        /// Eine Func als Type Converter hinzufügen
        /// </summary>
        public ClassMappingSettings AddConverter(Type tIn, Type tOut, Func<object, object> fn = null, bool allowAssignableInputs = false)
        {
            SimpleFuncConverter converter = new SimpleFuncConverter(tIn, tOut, null, allowAssignableInputs);
            converter.SetConverterFunc(fn ?? (m => m.MapTo(tOut, RemoveConverter(converter)))); // Eigenen converter eintfernen (sonst droht stackoverflow)
            return AddConverter(converter);
        }

        /// <summary>
        /// Fügt alle möglichen Converter der liste der zu benutzenden Konverter hinzu
        /// </summary>
        public ClassMappingSettings AddAllLoadedTypeConverters()
        {
            foreach (TypeConverter converter in RetrieveLoadedAssembliesTypeConverters())
                TypeConverters.Add(converter);
            return this;
        }

        /// <summary>
        /// Liefert Alle TypeConverter
        /// </summary>
        /// <returns></returns>
        public IList<TypeConverter> RetrieveLoadedAssembliesTypeConverters()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.GetName().Version > new Version(0, 0, 0, 0)).ToList();
            assemblies.Add(typeof(BooleanConverter).Assembly);

            foreach (Assembly assembly in assemblies)
            {
                if (!assembliesTypeConverters.ContainsKey(assembly))
                {
                    try
                    {
                        var currentAssemblyConverterList = new List<TypeConverter>();
                        foreach (Type converterType in assembly.GetTypes().Where(type => typeof(TypeConverter).IsAssignableFrom(type)))
                        {

                            try
                            {
                                TypeConverter typeConverter = ReflectionHelper.CreateInstance(converterType, false, false) as TypeConverter;
                                if (typeConverter != null)
                                {
                                    currentAssemblyConverterList.Add(typeConverter);
                                }
                            }
                            catch { }
                        }
                        assembliesTypeConverters.Add(assembly, currentAssemblyConverterList);
                    }
                    catch { }
                }
            }
            return assembliesTypeConverters.Values.SelectMany(list => list).ToList();
        }

    }

}