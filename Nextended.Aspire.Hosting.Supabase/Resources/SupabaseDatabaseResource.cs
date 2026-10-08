using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.Supabase.Resources;

/// <summary>
/// Represents a Supabase PostgreSQL database container resource.
/// </summary>
public sealed class SupabaseDatabaseResource : ContainerResource
{
    /// <summary>
    /// Creates a new instance of the SupabaseDatabaseResource.
    /// </summary>
    /// <param name="name">The name of the database container.</param>
    public SupabaseDatabaseResource(string name) : base(name)
    {
    }

    internal StackSecret PasswordSecret { get; } = new("postgres-insecure-dev-password");

    /// <summary>
    /// Gets or sets the database password. Set from a parameter (<c>WithPassword(parameter)</c>)
    /// it is that parameter's configured value.
    /// </summary>
    public string Password
    {
        get => PasswordSecret.Value;
        internal set => PasswordSecret.Set(value);
    }

    /// <summary>The parameter the password comes from, if it was set from one.</summary>
    public ParameterResource? PasswordParameter => PasswordSecret.Parameter;

    /// <summary>
    /// Gets or sets the external port for PostgreSQL connections.
    /// </summary>
    public int ExternalPort { get; internal set; } = 54322;

    /// <summary>
    /// Gets or sets the reference to the parent stack.
    /// </summary>
    internal SupabaseStackResource? Stack { get; set; }
}
