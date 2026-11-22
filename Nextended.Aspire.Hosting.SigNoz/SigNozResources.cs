using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.SigNoz;

/// <summary>
/// SigNoz: the UI and API for traces, metrics, logs, dashboards and alerts. It is the hub of the stack —
/// ClickHouse, the collector and an optional sign-in proxy register themselves here and nest under it in the
/// Aspire dashboard.
/// </summary>
public sealed class SigNozResource(string name) : ContainerResource(name)
{
    /// <summary>Port of the UI and API inside the container.</summary>
    public const int DefaultTargetPort = 8080;

    /// <summary>Port the collector fetches its remote configuration from (OpAMP).</summary>
    public const int OpAmpTargetPort = 4320;

    public const string HttpEndpointName = "http";
    public const string OpAmpEndpointName = "opamp";

    /// <summary>The UI and API.</summary>
    public EndpointReference HttpEndpoint => new(this, HttpEndpointName);

    /// <summary>Where SigNoz keeps its telemetry.</summary>
    public SigNozClickHouseResource ClickHouse => Stack.ClickHouse.Resource;

    /// <summary>The OTLP receiver every service sends to.</summary>
    public SigNozCollectorResource Collector => Stack.Collector.Resource;

    /// <summary>The oauth2-proxy of <c>WithOidcLogin</c> in front of SigNoz; <c>null</c> without one.</summary>
    public ContainerResource? SignIn => Stack.SignIn?.Resource;

    internal SigNozStack Stack { get; set; } = null!;
}

/// <summary>ClickHouse with SigNoz's configuration and an embedded ClickHouse Keeper (no ZooKeeper container).</summary>
public sealed class SigNozClickHouseResource(string name) : ContainerResource(name)
{
    /// <summary>Native protocol port inside the container.</summary>
    public const int NativeTargetPort = 9000;

    /// <summary>HTTP port inside the container (health check).</summary>
    public const int HttpTargetPort = 8123;

    public const string NativeEndpointName = "native";
    public const string HttpEndpointName = "http";

    public EndpointReference NativeEndpoint => new(this, NativeEndpointName);
}

/// <summary>The SigNoz OpenTelemetry collector: receives OTLP from the services and writes to ClickHouse. It applies the schema migrations before it starts.</summary>
public sealed class SigNozCollectorResource(string name) : ContainerResource(name)
{
    public const int OtlpGrpcTargetPort = 4317;
    public const int OtlpHttpTargetPort = 4318;
    public const string OtlpGrpcEndpointName = "otlp-grpc";
    public const string OtlpHttpEndpointName = "otlp-http";

    /// <summary>OTLP over gRPC; not published in a deployment.</summary>
    public EndpointReference OtlpGrpcEndpoint => new(this, OtlpGrpcEndpointName);

    /// <summary>OTLP over HTTP (<c>/v1/traces</c>, <c>/v1/metrics</c>, <c>/v1/logs</c>).</summary>
    public EndpointReference OtlpHttpEndpoint => new(this, OtlpHttpEndpointName);
}
