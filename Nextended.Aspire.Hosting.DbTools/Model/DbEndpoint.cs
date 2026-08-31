using System.Data.Common;
using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.DbTools;

/// <summary>
/// Where a database is and how to get in.
/// </summary>
/// <remarks>
/// Three kinds of source arrive here and have to come out as something one shell script can use:
///
/// * a resource in this stack, whose host is a container name and whose password is a parameter
///   that is not resolved until the stack runs — every part known separately;
/// * a connection string somebody typed for a server Aspire knows nothing about — known now, so it
///   is taken apart here;
/// * a parameter or an `AddConnectionString` resource — one string, and not known until the stack
///   runs, so it goes to the container whole and the script takes it apart there.
///
/// The first two become <see cref="Parts"/>; the third becomes <see cref="Whole"/>.
/// </remarks>
public sealed record DbEndpoint
{
    private DbEndpoint(ReferenceExpression? whole, ReferenceExpression? host, ReferenceExpression? port,
        ReferenceExpression? user, ReferenceExpression? password, ReferenceExpression? database)
    {
        WholeString = whole;
        Host = host;
        Port = port;
        User = user;
        Password = password;
        Database = database;
    }

    /// <summary>
    /// The whole connection string, for the kind that is not known until the stack runs. Null when
    /// the parts are known.
    /// </summary>
    public ReferenceExpression? WholeString { get; }

    /// <summary>The host the database is on. Null when only <see cref="WholeString"/> is known.</summary>
    public ReferenceExpression? Host { get; }

    /// <summary>The port it listens on — the engine's usual one where the source did not say.</summary>
    public ReferenceExpression? Port { get; }

    /// <summary>The user to connect as.</summary>
    public ReferenceExpression? User { get; }

    /// <summary>That user's password.</summary>
    public ReferenceExpression? Password { get; }

    /// <summary>The database to read or write. Redis has none, and leaves it empty.</summary>
    public ReferenceExpression? Database { get; }

    /// <summary>
    /// True when this is one whole connection string for the script to take apart, false when the
    /// parts are already known here.
    /// </summary>
    public bool IsWhole => WholeString is not null;

    /// <summary>Every part known separately — a resource in this stack, or a string already taken
    /// apart.</summary>
    public static DbEndpoint Parts(ReferenceExpression host, ReferenceExpression port,
        ReferenceExpression user, ReferenceExpression password, ReferenceExpression database) =>
        new(null, host, port, user, password, database);

    /// <summary>One string, taken apart by the script rather than here.</summary>
    public static DbEndpoint Whole(ReferenceExpression connectionString) =>
        new(connectionString, null, null, null, null, null);

    /// <summary>
    /// Everything as literal text — what a test reads back, and what a typed source with no
    /// password looks like.
    /// </summary>
    public static DbEndpoint Of(string? host, string? port, string? user, string? password,
        string? database) =>
        Parts(Literal(host), Literal(port), Literal(user), Literal(password), Literal(database));

    /// <summary>One value as a reference expression, empty where there is none.</summary>
    public static ReferenceExpression Literal(string? value) =>
        ReferenceExpression.Create($"{value ?? string.Empty}");

    /// <summary>
    /// A connection string as the parts its engine's command line wants.
    /// </summary>
    /// <remarks>
    /// Both forms people write: the ADO.NET one (`Server=…;Database=…;User Id=…`), where the same
    /// field has three spellings depending on which provider's documentation somebody had open, and
    /// the URI one (`postgres://user:pw@host:5432/db`) that every engine's own client accepts.
    ///
    /// `defaultPort` is what the engine listens on when the string does not say — every tool needs a
    /// port, and "the usual one" is the answer nobody should have to type.
    /// </remarks>
    public static DbEndpoint Parse(string connectionString, int defaultPort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (connectionString.Contains("://", StringComparison.Ordinal))
            return FromUri(connectionString, defaultPort);

        // DbConnectionStringBuilder parses the ADO.NET form without knowing any provider's keys,
        // which is exactly what is wanted: five fields, any provider's spelling of them.
        var parts = new DbConnectionStringBuilder { ConnectionString = connectionString };

        string? Value(params string[] keys)
        {
            foreach (var key in keys)
                if (parts.TryGetValue(key, out var value) && value?.ToString() is { Length: > 0 } text)
                    return text;

            return null;
        }

        var host = Value("Host", "Server", "Data Source", "Address", "Addr", "Network Address");
        var port = Value("Port");

        // "Server=localhost,1433" is how SQL Server writes a port, and "host:5432" happens too.
        if (host is { Length: > 0 } && port is null)
        {
            var at = host.LastIndexOfAny([',', ':']);

            if (at > 0 && int.TryParse(host[(at + 1)..], out _))
            {
                port = host[(at + 1)..];
                host = host[..at];
            }
        }

        return Of(host,
            port ?? defaultPort.ToString(),
            Value("Username", "User ID", "User Id", "UserId", "Uid", "User"),
            Value("Password", "Pwd"),
            Value("Database", "Initial Catalog", "Db"));
    }

    private static DbEndpoint FromUri(string value, int defaultPort)
    {
        var uri = new Uri(value);
        var credentials = uri.UserInfo.Split(':', 2);

        return Of(uri.Host,
            uri.IsDefaultPort || uri.Port <= 0 ? defaultPort.ToString() : uri.Port.ToString(),
            credentials.Length > 0 && credentials[0].Length > 0
                ? Uri.UnescapeDataString(credentials[0])
                : null,
            credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null,
            uri.AbsolutePath.Trim('/') is { Length: > 0 } path ? Uri.UnescapeDataString(path) : null);
    }
}
