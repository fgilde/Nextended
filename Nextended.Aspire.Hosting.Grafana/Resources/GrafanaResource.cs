using Aspire.Hosting.ApplicationModel;

namespace Nextended.Aspire.Hosting.Grafana;

/// <summary>
/// Grafana dashboards UI. Acts as the hub of the observability stack: every
/// component added via <c>WithPrometheus</c> / <c>WithLoki</c> / <c>WithTempo</c> /
/// <c>WithOtelCollector</c> / … registers itself here and is auto-provisioned as a
/// Grafana datasource. Configs are generated at application start (not at builder
/// time), so the <c>With*</c> calls can come in any order.
/// </summary>
public sealed class GrafanaResource(string name) : ContainerResource(name)
{
    /// <summary>Default internal container port Grafana listens on.</summary>
    public const int DefaultTargetPort = 3000;

    /// <summary>Name of the primary HTTP endpoint.</summary>
    public const string HttpEndpointName = "http";

    /// <summary>The HTTP endpoint serving the Grafana UI.</summary>
    public EndpointReference HttpEndpoint => new(this, HttpEndpointName);

    /// <summary>Shared state of the stack this Grafana instance belongs to.</summary>
    internal ObservabilityStackContext Context { get; set; } = null!;
}

/// <summary>Prometheus metrics database (scraper + TSDB + remote-write receiver).</summary>
public sealed class PrometheusResource(string name) : ContainerResource(name)
{
    public const int DefaultTargetPort = 9090;
    public const string HttpEndpointName = "http";

    public EndpointReference HttpEndpoint => new(this, HttpEndpointName);

    internal ObservabilityStackContext Context { get; set; } = null!;
}

/// <summary>Loki log aggregation backend.</summary>
public sealed class LokiResource(string name) : ContainerResource(name)
{
    public const int DefaultTargetPort = 3100;
    public const string HttpEndpointName = "http";

    public EndpointReference HttpEndpoint => new(this, HttpEndpointName);

    internal ObservabilityStackContext Context { get; set; } = null!;
}

/// <summary>Tempo distributed-tracing backend (OTLP receiver + trace store).</summary>
public sealed class TempoResource(string name) : ContainerResource(name)
{
    public const int DefaultTargetPort = 3200;
    public const string HttpEndpointName = "http";
    public const string OtlpGrpcEndpointName = "otlp-grpc";

    public EndpointReference HttpEndpoint => new(this, HttpEndpointName);

    internal ObservabilityStackContext Context { get; set; } = null!;
}

/// <summary>OpenTelemetry Collector — central OTLP receiver that fans out to the stack's backends.</summary>
public sealed class OtelCollectorResource(string name) : ContainerResource(name)
{
    public const int OtlpGrpcTargetPort = 4317;
    public const int OtlpHttpTargetPort = 4318;
    public const string OtlpGrpcEndpointName = "otlp-grpc";
    public const string OtlpHttpEndpointName = "otlp-http";

    public EndpointReference OtlpGrpcEndpoint => new(this, OtlpGrpcEndpointName);
    public EndpointReference OtlpHttpEndpoint => new(this, OtlpHttpEndpointName);

    internal ObservabilityStackContext Context { get; set; } = null!;
}

/// <summary>Promtail — ships Docker container logs to Loki (local-only, needs the Docker socket).</summary>
public sealed class PromtailResource(string name) : ContainerResource(name);

/// <summary>cAdvisor — per-container CPU/Memory/Network metrics (local-only, needs the Docker socket).</summary>
public sealed class CAdvisorResource(string name) : ContainerResource(name)
{
    public const int DefaultTargetPort = 8080;
}

/// <summary>postgres_exporter sidecar — Postgres internals as Prometheus metrics.</summary>
public sealed class PostgresExporterResource(string name) : ContainerResource(name)
{
    public const int DefaultTargetPort = 9187;
}
