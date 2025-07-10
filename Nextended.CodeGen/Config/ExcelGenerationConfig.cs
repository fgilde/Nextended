using System.Collections.Generic;

namespace Nextended.CodeGen.Config;

public class ExcelGenerationConfig: CodeGenerationConfigBase
{
    public string SourceFile { get; set; } = "";
    public string? OutputPath { get; set; }
    
    /* --- Sheet und Struktur -------------------------------------------- */
    public string? SheetName { get; set; }                // Standard: 1. Blatt
    public int HeaderRowIndex { get; set; } = 1;          // 1‑basiert
    public int DataStartRowIndex { get; set; } = 2;       // 1‑basiert
    public string KeyColumn { get; set; } = "A"; // Spalte für Property‑Namen

    /* --- Modell‑Klasse -------------------------------------------------- */
    public bool GenerateModelClass { get; set; } = true;
    public string RootClassName { get; set; } = "ExcelRow";
    public string? ModelClassPrefix { get; set; }       // z. B. "Cfg"
    public IDictionary<string, string>? ColumnMappings { get; set; }
    // Key = Excel‑Header (oder A, B, C …), Value = Property‑Name

    public IDictionary<string, string>? PropertyTypeOverrides { get; set; }
    // Key = Property‑Name nach obiger Auflösung, Value = C#‑Typ (z. B. "int", "DateTime")

    /* --- Statische Lookup‑Klasse --------------------------------------- */
    public bool GenerateStaticTable { get; set; } = true;
    public string StaticClassName { get; set; } = "Rows";
    public string? StaticPropertyPrefix { get; set; }     // z. B. "" oder "Row"
    public bool GenerateAllCollection { get; set; } = true;
}