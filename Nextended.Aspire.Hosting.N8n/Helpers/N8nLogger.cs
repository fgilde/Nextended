using Microsoft.Extensions.Logging;

namespace Nextended.Aspire.Hosting.N8n.Helpers;

/// <summary>
/// Provides centralized logging for the n8n Aspire integration.
/// Uses an <see cref="ILoggerFactory"/> when available, falls back to debug output.
/// </summary>
internal static class N8nLogger
{
    private static ILogger? _logger;
    private static readonly object Lock = new();
    private const string Prefix = "[n8n]";

    /// <summary>
    /// Initializes the logger with an <see cref="ILoggerFactory"/>.
    /// </summary>
    public static void Initialize(ILoggerFactory loggerFactory)
    {
        lock (Lock)
        {
            _logger = loggerFactory.CreateLogger("n8n");
        }
    }

    /// <summary>Logs an informational message.</summary>
    public static void LogInformation(string message)
    {
        if (_logger is not null)
            _logger.LogInformation("{Message}", message);
        else
            System.Diagnostics.Debug.WriteLine($"{Prefix} {message}");
    }

    /// <summary>Logs a warning message.</summary>
    public static void LogWarning(string message)
    {
        if (_logger is not null)
            _logger.LogWarning("{Message}", message);
        else
            System.Diagnostics.Debug.WriteLine($"{Prefix} WARNING: {message}");
    }

    /// <summary>Logs an error message.</summary>
    public static void LogError(string message)
    {
        if (_logger is not null)
            _logger.LogError("{Message}", message);
        else
            System.Diagnostics.Debug.WriteLine($"{Prefix} ERROR: {message}");
    }
}
