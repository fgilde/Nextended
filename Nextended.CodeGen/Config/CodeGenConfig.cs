namespace Nextended.CodeGen.Config;

public class CodeGenConfig
{
    public List<ExcelFileConfig> ExcelFiles { get; set; } = new();
    public List<JsonFileConfig> JsonFiles { get; set; } = new();
}

public class ExcelFileConfig
{
    public string File { get; set; }
    public string Sheet { get; set; }
    public string Namespace { get; set; }
}

public class JsonFileConfig
{
    public string File { get; set; }
    public string RootType { get; set; }
    public string Namespace { get; set; }
}


