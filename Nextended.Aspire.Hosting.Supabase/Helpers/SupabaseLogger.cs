using Microsoft.Extensions.Logging;

namespace Nextended.Aspire.Hosting.Supabase.Helpers;

/// <summary>
/// Provides centralized logging for Supabase components.
/// Uses ILoggerFactory when available, falls back to console output.
/// </summary>
internal static class SupabaseLogger
{
    private static ILogger? _logger;
    private static readonly object _lock = new();
    private const string Prefix = "[Supabase]";
    private const string SyncPrefix = "[Supabase Sync]";

    /// <summary>
    /// Initializes the logger with an ILoggerFactory.
    /// Call this during application startup if logging infrastructure is available.
    /// </summary>
    public static void Initialize(ILoggerFactory loggerFactory)
    {
        lock (_lock)
        {
            _logger = loggerFactory.CreateLogger("Supabase");
        }
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    public static void LogInformation(string message)
    {
        if (_logger is not null)
        {
            _logger.LogInformation("{Message}", message);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"{Prefix} {message}");
        }
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public static void LogWarning(string message)
    {
        if (_logger is not null)
        {
            _logger.LogWarning("{Message}", message);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"{Prefix} WARNING: {message}");
        }
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    public static void LogError(string message)
    {
        if (_logger is not null)
        {
            _logger.LogError("{Message}", message);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"{Prefix} ERROR: {message}");
        }
    }

    /// <summary>
    /// Logs an error message with exception details.
    /// </summary>
    public static void LogError(Exception ex, string message)
    {
        if (_logger is not null)
        {
            _logger.LogError(ex, "{Message}", message);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"{Prefix} ERROR: {message}");
            System.Diagnostics.Debug.WriteLine($"{Prefix} Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs an informational message for sync operations.
    /// </summary>
    public static void LogSyncInfo(string message)
    {
        if (_logger is not null)
        {
            _logger.LogInformation("{Prefix} {Message}", SyncPrefix, message);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"{SyncPrefix} {message}");
        }
    }

    /// <summary>
    /// Logs a warning message for sync operations.
    /// </summary>
    public static void LogSyncWarning(string message)
    {
        if (_logger is not null)
        {
            _logger.LogWarning("{Prefix} {Message}", SyncPrefix, message);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"{SyncPrefix} WARNING: {message}");
        }
    }

    /// <summary>
    /// Logs an error message for sync operations.
    /// </summary>
    public static void LogSyncError(string message)
    {
        if (_logger is not null)
        {
            _logger.LogError("{Prefix} {Message}", SyncPrefix, message);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"{SyncPrefix} ERROR: {message}");
        }
    }

    /// <summary>
    /// Logs a debug message (only shown when DEBUG is defined).
    /// </summary>
    [System.Diagnostics.Conditional("DEBUG")]
    public static void LogDebug(string message)
    {
        if (_logger is not null)
        {
            _logger.LogDebug("{Message}", message);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"{Prefix} DEBUG: {message}");
        }
    }
}
