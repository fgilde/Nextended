namespace Nextended.CodeGen.Attributes;

/// <summary>
/// Kann bei einer Klasse die das AutoGenerateComAttribute enthält auf eine Property gesetzt werden um den Typen zu spezifizieren 
/// der für das Com Interface und die Com Klasse benutzt werden soll, oder abweichende Namen anzugeben
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class GenerationPropertySettingAttribute : Attribute
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
    public GenerationPropertySettingAttribute()
    { }

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="T:System.Attribute"/>-Klasse.
    /// </summary>
    public GenerationPropertySettingAttribute(Type type)
    {
        Type = type;
    }

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="T:System.Attribute"/>-Klasse.
    /// </summary>
    public GenerationPropertySettingAttribute(string comPropertyName)
    {
        PropertyName = comPropertyName;
    }

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="T:System.Attribute"/>-Klasse.
    /// </summary>
    public GenerationPropertySettingAttribute(string comPropertyName, Type type)
    {
        PropertyName = comPropertyName;
        Type = type;
    }
}