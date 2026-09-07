namespace Nextended.Aspire.Hosting.AspireUI;

/// <summary>
/// Everything under AspireUI's own settings, as properties instead of key strings. Anything left
/// null is not sent, and a value that is sent only fills in what the instance has not got yet —
/// unless <see cref="ForceOnEveryStart"/> says otherwise, which also overwrites what somebody
/// changed in the UI.
/// <para>
/// This is the wide, typed way; <c>WithSetting(key, value)</c> stays for anything not listed here,
/// and single-subject methods (<c>WithSingleSignOn</c>, <c>WithS3Backups</c>, …) stay for the
/// settings that come as a set.
/// </para>
/// </summary>
public sealed class AspireUISettings
{
    // --- This machine -----------------------------------------------------------------------
    /// <summary>The host name every app url is built from. Blank uses the host the browser asked for.</summary>
    public string? PublicHost { get; set; }

    // --- The bundled Aspire dashboard --------------------------------------------------------
    /// <summary>Host an Aspire dashboard next to every deployed app.</summary>
    public bool? HostDashboards { get; set; }

    /// <summary>A fixed browser token, so AspireUI can hand out a one-click login link to it.</summary>
    public string? DashboardToken { get; set; }

    // --- Proxy (Nginx Proxy Manager) ---------------------------------------------------------
    public bool? ProxyEnabled { get; set; }
    public string? ProxyBaseUrl { get; set; }
    public string? ProxyEmail { get; set; }
    public string? ProxyPassword { get; set; }

    /// <summary>Where the proxy forwards to. Blank uses <see cref="PublicHost"/>.</summary>
    public string? ProxyForwardHost { get; set; }

    // --- Notifications -----------------------------------------------------------------------
    /// <summary>Slack, Discord, Teams, ntfy — anything that takes a json post.</summary>
    public string? NotifyWebhookUrl { get; set; }
    public string? NotifyTelegramToken { get; set; }
    public string? NotifyTelegramChat { get; set; }

    // --- Backups -----------------------------------------------------------------------------
    /// <summary>Back up every running app's volumes this often. Zero turns the schedule off.</summary>
    public int? BackupIntervalHours { get; set; }

    /// <summary>How many snapshots to keep per app.</summary>
    public int? BackupRetain { get; set; }

    // --- Import ------------------------------------------------------------------------------
    /// <summary>Files larger than this are skipped when a folder or archive is imported.</summary>
    public int? MaxImportFileMb { get; set; }

    /// <summary>Skip what an imported folder's own .gitignore ignores.</summary>
    public bool? RespectGitignore { get; set; }

    // --- Activity log ------------------------------------------------------------------------
    /// <summary>How long an entry is kept. Zero keeps everything.</summary>
    public int? AuditRetainDays { get; set; }

    // --- The assistant -----------------------------------------------------------------------
    /// <summary><c>http</c> for an OpenAI-compatible endpoint, <c>cli</c> for a local agent CLI.</summary>
    public string? AiKind { get; set; }
    public string? AiBaseUrl { get; set; }
    public string? AiApiKey { get; set; }
    public string? AiModel { get; set; }

    /// <summary>What the UI calls the provider, e.g. "Ollama".</summary>
    public string? AiProviderLabel { get; set; }

    /// <summary>The installed CLI to use when <see cref="AiKind"/> is <c>cli</c>: claude, gemini, ollama, llm, codex.</summary>
    public string? AiCliTool { get; set; }

    /// <summary>
    /// Apply these on every start instead of only filling in what is empty. Off by default, because
    /// it also overwrites what somebody changed in the UI.
    /// </summary>
    public bool ForceOnEveryStart { get; set; }

    /// <summary>
    /// The settings as AspireUI's own keys. One place says how a property is spelled on the other
    /// side, so renaming a property here cannot silently stop writing a setting there.
    /// </summary>
    internal IEnumerable<KeyValuePair<string, string>> AsSettings()
    {
        var map = new (string Key, object? Value)[]
        {
            ("PublicHost", PublicHost),
            ("HostDashboard", HostDashboards),
            ("DashboardToken", DashboardToken),
            ("NpmEnabled", ProxyEnabled),
            ("NpmBaseUrl", ProxyBaseUrl),
            ("NpmEmail", ProxyEmail),
            ("NpmPassword", ProxyPassword),
            ("NpmForwardHost", ProxyForwardHost),
            ("NotifyWebhookUrl", NotifyWebhookUrl),
            ("NotifyTelegramToken", NotifyTelegramToken),
            ("NotifyTelegramChat", NotifyTelegramChat),
            ("BackupIntervalHours", BackupIntervalHours),
            ("BackupRetain", BackupRetain),
            ("MaxImportFileMb", MaxImportFileMb),
            ("RespectGitignore", RespectGitignore),
            ("AuditRetainDays", AuditRetainDays),
            ("AiKind", AiKind),
            ("AiBaseUrl", AiBaseUrl),
            ("AiApiKey", AiApiKey),
            ("AiModel", AiModel),
            ("AiProviderLabel", AiProviderLabel),
            ("AiCliTool", AiCliTool),
        };

        foreach (var (key, value) in map)
        {
            var text = value switch
            {
                null => null,
                bool b => b ? "true" : "false",
                string s => string.IsNullOrWhiteSpace(s) ? null : s.Trim(),
                _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
            };
            if (text is not null) yield return new KeyValuePair<string, string>(key, text);
        }
    }
}
