namespace Nextended.Aspire.Hosting.Php;

/// <summary>
/// Names the php.ini directive a <see cref="PhpIniConfiguration"/> property maps to, for
/// directives the PascalCase→snake_case convention can't produce (e.g. <c>date.timezone</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PhpIniKeyAttribute(string key) : Attribute
{
    /// <summary>The php.ini directive name.</summary>
    public string Key { get; } = key;
}

/// <summary>
/// Typed php.ini settings for <c>WithPhpIniConfiguration</c>. Only assigned (non-null) properties
/// are applied. Property names map to directives by convention (<c>DisplayErrors</c> →
/// <c>display_errors</c>); <see cref="PhpIniKeyAttribute"/> overrides where the convention doesn't
/// fit. Booleans become <c>1</c>/<c>0</c>. The mapping is purely reflective — subclass this and add
/// your own properties for any directive not listed here.
/// </summary>
public class PhpIniConfiguration
{
    /// <summary><c>display_errors</c> — print errors as part of the output (dev setting).</summary>
    public bool? DisplayErrors { get; set; }

    /// <summary><c>display_startup_errors</c> — also show errors from PHP's startup sequence.</summary>
    public bool? DisplayStartupErrors { get; set; }

    /// <summary><c>error_reporting</c> — passed verbatim (e.g. <c>"E_ALL"</c> or a numeric value like <c>"32767"</c>).</summary>
    public string? ErrorReporting { get; set; }

    /// <summary><c>log_errors</c> — log errors to the server log (visible in the Aspire console).</summary>
    public bool? LogErrors { get; set; }

    /// <summary><c>memory_limit</c> — e.g. <c>"256M"</c>.</summary>
    public string? MemoryLimit { get; set; }

    /// <summary><c>max_execution_time</c> — script timeout in seconds.</summary>
    public int? MaxExecutionTime { get; set; }

    /// <summary><c>max_input_time</c> — input parsing timeout in seconds.</summary>
    public int? MaxInputTime { get; set; }

    /// <summary><c>max_input_vars</c> — maximum number of accepted input variables.</summary>
    public int? MaxInputVars { get; set; }

    /// <summary><c>post_max_size</c> — e.g. <c>"64M"</c>.</summary>
    public string? PostMaxSize { get; set; }

    /// <summary><c>upload_max_filesize</c> — e.g. <c>"32M"</c>.</summary>
    public string? UploadMaxFilesize { get; set; }

    /// <summary><c>max_file_uploads</c> — maximum simultaneous file uploads.</summary>
    public int? MaxFileUploads { get; set; }

    /// <summary><c>file_uploads</c> — allow HTTP file uploads at all.</summary>
    public bool? FileUploads { get; set; }

    /// <summary><c>default_charset</c> — e.g. <c>"UTF-8"</c>.</summary>
    public string? DefaultCharset { get; set; }

    /// <summary><c>short_open_tag</c> — allow <c>&lt;?</c> as PHP open tag.</summary>
    public bool? ShortOpenTag { get; set; }

    /// <summary><c>date.timezone</c> — e.g. <c>"Europe/Berlin"</c>.</summary>
    [PhpIniKey("date.timezone")]
    public string? DateTimezone { get; set; }

    /// <summary><c>session.save_path</c> — where session files are stored inside the container.</summary>
    [PhpIniKey("session.save_path")]
    public string? SessionSavePath { get; set; }
}
