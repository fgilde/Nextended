namespace Nextended.CodeGen.Attributes
{

    /// <summary>
    /// Attribute das dafür sorgt das für die klasse auf dem es gesetzt ist automatisch ein ComInterface und eine Com Klasse erzeugt werden
    /// (Erzeugt per T4-Template zur Compiletime die Com Klassen)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = true)]
    public class AutoGenerateComAttribute : Attribute
    {
        /// <summary>
        /// Wird bei der erstellten Klasse und Interface als Prefix im Namen benutzt (z-B IComMyType)
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Wird bei der erstellten Klasse und Interface als Suffix im Namen benutzt (z-B IComMyTypeSuffix)
        /// </summary>        
        public string Suffix { get; set; }

        /// <summary>
        /// Mit dieser Eigenschaft kann der klassenname der erzeugten Klasse überschreieben werden (dann greift keine Suffix u Prefix logik)
        /// </summary>        
        public string ComClassName { get; set; }

        /// <summary>
        /// Gibt an ob das Attribute "ToNetMappingAttribute" für die automatische erstellung der COM -> .NET Konvertierung erzeugt werden soll
        /// </summary>        
        public bool GenerateToNetMapping { get; set; }

        /// <summary>
        /// Gets or sets the name of the generic parameter type.
        /// </summary>       
        public Type[] GenericParameterTypes { get; set; }

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="T:System.Attribute"/>-Klasse.
        /// </summary>
        public AutoGenerateComAttribute(Type genericTypeDescription)
            : this()
        {
            GenericParameterTypes = genericTypeDescription.GetGenericArguments();
        }

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="T:System.Attribute"/>-Klasse.
        /// </summary>
        public AutoGenerateComAttribute()
        {
            Prefix = "Com";
            Suffix = "";
            GenerateToNetMapping = true;
        }
    }

    /// <summary>
    /// Kann bei einer Klasse die das AutoGenerateComAttribute enthält auf eine Property gesetzt werden um den Typen zu spezifizieren 
    /// der für das Com Interface und die Com Klasse benutzt werden soll, oder abweichende Namen anzugeben
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ComPropertySettingAttribute : Attribute
    {
        /// <summary>        
        /// Type der für das Com Interface und die Com Klasse benutzt werden soll
        /// </summary>
        public Type Type { get; set; }

        /// <summary>        
        /// Type der für das Com Interface und die Com Klasse benutzt werden soll
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Is diese Eigenschaft auf true, wird für die Property der Classmapper benutzt wenn die Klasse ein automatisches .Net mapping erzeugt
        /// </summary>
        public bool MapWithClassMapper { get; set; }

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="T:System.Attribute"/>-Klasse.
        /// </summary>
        public ComPropertySettingAttribute()
        { }

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="T:System.Attribute"/>-Klasse.
        /// </summary>
        public ComPropertySettingAttribute(Type type)
        {
            Type = type;
        }

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="T:System.Attribute"/>-Klasse.
        /// </summary>
        public ComPropertySettingAttribute(string comPropertyName)
        {
            PropertyName = comPropertyName;
        }

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="T:System.Attribute"/>-Klasse.
        /// </summary>
        public ComPropertySettingAttribute(string comPropertyName, Type type)
        {
            PropertyName = comPropertyName;
            Type = type;
        }
    }



    /// <summary>
    /// Kann bei einer Klasse die das AutoGenerateComAttribute enthält auf eine Property gesetzt werden diese für die COM Generierung zu ignorieren    
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ComIgnoreAttribute : Attribute
    { }


}