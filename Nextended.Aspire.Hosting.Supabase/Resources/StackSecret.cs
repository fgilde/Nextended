using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.Supabase.Resources;

/// <summary>
/// A secret of the stack: a plain value (local development) or an Aspire parameter. A parameter
/// stays a reference into the deployment — Azure Container Apps get it as a secret, never as
/// plain text — and no consumer reads it before the environment is evaluated, so it may be set
/// after the containers that use it exist.
/// </summary>
internal sealed class StackSecret(string value)
{
    private string _value = value;

    public ParameterResource? Parameter { get; private set; }

    public void Set(string value)
    {
        _value = value;
        Parameter = null;
    }

    public void Set(ParameterResource parameter) => Parameter = parameter;

    /// <summary>The value itself; a parameter without a configured value throws (Aspire's own message).</summary>
#pragma warning disable CS0618 // the synchronous accessor is what the model-building code paths need
    public string Value => Parameter?.Value ?? _value;
#pragma warning restore CS0618

    /// <summary>For an environment variable: the parameter itself, or the plain value.</summary>
    public object EnvironmentValue => (object?)Parameter ?? _value;

    /// <summary>For use inside a connection string or another expression.</summary>
    public ReferenceExpression Expression => Parameter is { } parameter
        ? ReferenceExpression.Create($"{parameter}")
        : ReferenceExpression.Create($"{_value}");
}
