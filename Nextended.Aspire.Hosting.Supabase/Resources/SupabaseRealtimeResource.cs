using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.Supabase.Resources;

/// <summary>
/// Represents a Supabase Realtime container resource.
/// </summary>
public sealed class SupabaseRealtimeResource : ContainerResource
{
    /// <summary>
    /// Creates a new instance of the SupabaseRealtimeResource.
    /// </summary>
    /// <param name="name">The name of the realtime container.</param>
    public SupabaseRealtimeResource(string name) : base(name)
    {
    }

    /// <summary>
    /// Gets or sets the port the Realtime service listens on.
    /// </summary>
    public int Port { get; internal set; } = 4000;

    /// <summary>
    /// Gets or sets the reference to the parent stack.
    /// </summary>
    internal SupabaseStackResource? Stack { get; set; }
}
