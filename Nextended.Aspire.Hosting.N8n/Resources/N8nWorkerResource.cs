using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.N8n.Resources;

/// <summary>
/// Represents an n8n worker container resource used in queue mode.
/// Workers share the n8n image, encryption key, database and Redis with the main instance,
/// but run the <c>worker</c> command to process executions from the queue.
/// </summary>
public sealed class N8nWorkerResource : ContainerResource
{
    /// <summary>
    /// Creates a new instance of the <see cref="N8nWorkerResource"/>.
    /// </summary>
    /// <param name="name">The name of the worker container.</param>
    public N8nWorkerResource(string name) : base(name)
    {
    }

    /// <summary>Gets the reference to the parent n8n resource.</summary>
    internal N8nResource? Parent { get; set; }
}
