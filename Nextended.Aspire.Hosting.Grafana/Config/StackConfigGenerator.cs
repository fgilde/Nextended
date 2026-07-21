using System.Globalization;
using System.Text;

namespace Nextended.Aspire.Hosting.Grafana;

/// <summary>
/// Generates the YAML config files for the observability stack from runtime
/// values (resource names, registered datasources, scrape jobs). The generators
/// are deliberately simple string interpolation — no template engine, no
/// dependency. They're meant to be readable side-by-side with the YAML they
/// produce.
/// </summary>
internal static class StackConfigGenerator
{
    // -------------------------------------------------------------------------
    // Prometheus
    // -------------------------------------------------------------------------
    public static string GetPrometheusYaml(IEnumerable<PrometheusScrapeJob> scrapeJobs, string retention)
    {
        var jobs = new List<string>
        {
            $$"""
              - job_name: prometheus
                static_configs:
                  - targets: ['localhost:9090']
            """,
        };

        foreach (var job in scrapeJobs)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"  - job_name: {job.JobName}");
            if (job.MetricsPath is not null)
                sb.AppendLine($"    metrics_path: {job.MetricsPath}");
            sb.AppendLine("    static_configs:");
            sb.Append($"      - targets: ['{job.Target}']");
            if (job.ExtraYaml is not null)
            {
                sb.AppendLine();
                sb.Append(Indent(job.ExtraYaml.TrimEnd(), "    "));
            }
            jobs.Add(sb.ToString());
        }

        return $"""
            global:
              scrape_interval: 15s
              evaluation_interval: 15s

            scrape_configs:
            {string.Join("\n", jobs)}
            """;
    }

    // -------------------------------------------------------------------------
    // Loki (single-binary, filesystem-backed)
    // -------------------------------------------------------------------------
    public static string GetLokiConfigYaml() => """
        auth_enabled: false

        server:
          http_listen_port: 3100
          grpc_listen_port: 9096
          log_level: warn

        common:
          instance_addr: 127.0.0.1
          path_prefix: /loki
          storage:
            filesystem:
              chunks_directory: /loki/chunks
              rules_directory: /loki/rules
          replication_factor: 1
          ring:
            kvstore:
              store: inmemory

        schema_config:
          configs:
            - from: 2024-01-01
              store: tsdb
              object_store: filesystem
              schema: v13
              index:
                prefix: index_
                period: 24h

        limits_config:
          retention_period: 336h
          reject_old_samples: true
          reject_old_samples_max_age: 168h
          allow_structured_metadata: true

        compactor:
          working_directory: /loki/compactor
          retention_enabled: true
          retention_delete_delay: 2h
          delete_request_store: filesystem
        """;

    // -------------------------------------------------------------------------
    // Promtail (Docker socket scrape → Loki push)
    // -------------------------------------------------------------------------
    public static string GetPromtailConfigYaml(string lokiHost) => $$"""
        server:
          http_listen_port: 9080
          grpc_listen_port: 0
          log_level: warn

        positions:
          filename: /tmp/positions.yaml

        clients:
          - url: http://{{lokiHost}}:3100/loki/api/v1/push

        scrape_configs:
          - job_name: docker
            docker_sd_configs:
              - host: unix:///var/run/docker.sock
                refresh_interval: 5s

            relabel_configs:
              - source_labels: ['__meta_docker_container_name']
                regex: '/(.*)'
                target_label: container
              - source_labels: ['__meta_docker_container_image']
                target_label: image
              - source_labels: ['__meta_docker_container_label_com_docker_compose_service']
                target_label: aspire_resource

            pipeline_stages:
              - docker: {}
        """;

    // -------------------------------------------------------------------------
    // Tempo (single-binary, filesystem-backed, OTLP receiver)
    // -------------------------------------------------------------------------
    public static string GetTempoConfigYaml(string? prometheusHost)
    {
        // metrics_generator needs a Prometheus remote_write target — skip the whole
        // block (and its processors) when the stack runs without Prometheus.
        var metricsGenerator = prometheusHost is null ? "" : $"""


            metrics_generator:
              registry:
                external_labels:
                  source: tempo
              storage:
                path: /tmp/tempo/generator/wal
                remote_write:
                  - url: http://{prometheusHost}:9090/api/v1/write
                    send_exemplars: true
              traces_storage:
                path: /tmp/tempo/generator/traces
            """;

        var overrides = prometheusHost is null ? "" : """


            overrides:
              defaults:
                metrics_generator:
                  processors: ['service-graphs', 'span-metrics']
            """;

        return $"""
            server:
              http_listen_port: 3200
              grpc_listen_port: 9095

            distributor:
              receivers:
                otlp:
                  protocols:
                    grpc:
                      endpoint: 0.0.0.0:4317
                    http:
                      endpoint: 0.0.0.0:4318

            ingester:
              trace_idle_period: 10s
              max_block_duration: 5m

            compactor:
              compaction:
                block_retention: 168h{metricsGenerator}

            storage:
              trace:
                backend: local
                wal:
                  path: /tmp/tempo/wal
                local:
                  path: /tmp/tempo/blocks{overrides}
            """;
    }

    // -------------------------------------------------------------------------
    // OpenTelemetry Collector (central fan-out)
    // -------------------------------------------------------------------------
    public static string GetOtelCollectorConfigYaml(
        string? tempoHost, string? lokiHost, string? prometheusHost, string? aspireDashboardOtlpEndpoint)
    {
        // Build the exporter list dynamically — only include exporters whose
        // target service is actually in the stack. Avoids "connection refused"
        // noise in the collector logs when a backend isn't deployed.
        var tracesExporters = new List<string> { "debug" };
        var exporterDefs = new List<string>();

        if (!string.IsNullOrEmpty(tempoHost))
        {
            exporterDefs.Add($$"""
                  otlp/tempo:
                    endpoint: {{tempoHost}}:4317
                    tls:
                      insecure: true
                """);
            tracesExporters.Add("otlp/tempo");
        }

        // Aspire dashboard mirror — only when the endpoint is supplied. Lets
        // traces show up in the built-in dashboard alongside Grafana.
        if (!string.IsNullOrEmpty(aspireDashboardOtlpEndpoint))
        {
            exporterDefs.Add($$"""
                  otlp/aspire:
                    endpoint: {{aspireDashboardOtlpEndpoint}}
                    tls:
                      insecure: true
                """);
            tracesExporters.Add("otlp/aspire");
        }

        var logsExporters = new List<string> { "debug" };
        if (!string.IsNullOrEmpty(lokiHost))
        {
            exporterDefs.Add($$"""
                  otlphttp/loki:
                    endpoint: http://{{lokiHost}}:3100/otlp
                    tls:
                      insecure: true
                """);
            logsExporters.Add("otlphttp/loki");
        }

        var metricsExporters = new List<string> { "debug" };
        if (!string.IsNullOrEmpty(prometheusHost))
        {
            exporterDefs.Add($$"""
                  prometheusremotewrite:
                    endpoint: http://{{prometheusHost}}:9090/api/v1/write
                    tls:
                      insecure: true
                """);
            metricsExporters.Add("prometheusremotewrite");
        }

        return $$"""
            receivers:
              otlp:
                protocols:
                  grpc:
                    endpoint: 0.0.0.0:4317
                  http:
                    endpoint: 0.0.0.0:4318

            processors:
              batch:
                timeout: 5s
                send_batch_size: 512
              resource:
                attributes:
                  - key: deployment.environment
                    value: aspire-local
                    action: upsert

            exporters:
            {{string.Join("\n", exporterDefs)}}
              debug:
                # `detailed` logs the resource attributes + span names for each
                # received batch — invaluable when a service isn't actually
                # delivering. Tone down to `basic` once you're confident the
                # pipeline is healthy.
                verbosity: detailed

            service:
              telemetry:
                logs:
                  level: info
              pipelines:
                traces:
                  receivers: [otlp]
                  processors: [resource, batch]
                  exporters: [{{string.Join(", ", tracesExporters)}}]
                logs:
                  receivers: [otlp]
                  processors: [resource, batch]
                  exporters: [{{string.Join(", ", logsExporters)}}]
                metrics:
                  receivers: [otlp]
                  processors: [resource, batch]
                  exporters: [{{string.Join(", ", metricsExporters)}}]
            """;
    }

    // -------------------------------------------------------------------------
    // Grafana datasources (provisioning)
    // -------------------------------------------------------------------------
    public static string GetGrafanaDatasourcesYaml(IEnumerable<GrafanaDatasource> datasources)
    {
        var sb = new StringBuilder();
        sb.AppendLine("apiVersion: 1");
        sb.AppendLine();
        sb.AppendLine("datasources:");

        foreach (var ds in datasources)
        {
            sb.AppendLine($"  - name: {YamlScalar(ds.Name)}");
            sb.AppendLine($"    uid: {YamlScalar(ds.ResolvedUid)}");
            sb.AppendLine($"    type: {YamlScalar(ds.Type)}");
            sb.AppendLine($"    access: {YamlScalar(ds.Access)}");
            sb.AppendLine($"    url: {YamlScalar(ds.Url)}");
            if (ds.User is not null)
                sb.AppendLine($"    user: {YamlScalar(ds.User)}");
            if (ds.IsDefault)
                sb.AppendLine("    isDefault: true");
            sb.AppendLine($"    editable: {(ds.Editable ? "true" : "false")}");
            if (ds.JsonData.Count > 0)
            {
                sb.AppendLine("    jsonData:");
                AppendYaml(sb, ds.JsonData, "      ");
            }
            if (ds.SecureJsonData.Count > 0)
            {
                sb.AppendLine("    secureJsonData:");
                AppendYaml(sb, ds.SecureJsonData, "      ");
            }
        }

        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // Grafana dashboard provisioning (static — no host refs)
    // -------------------------------------------------------------------------
    public static string GetGrafanaDashboardsProvisioningYaml(string folderName = "Application") => $$"""
        apiVersion: 1

        providers:
          - name: {{folderName}}
            folder: {{folderName}}
            type: file
            disableDeletion: true
            editable: true
            updateIntervalSeconds: 30
            allowUiUpdates: true
            options:
              path: /var/lib/grafana/dashboards
              foldersFromFilesStructure: false
        """;

    // -------------------------------------------------------------------------
    // Minimal YAML emission for datasource jsonData/secureJsonData — scalars,
    // string-keyed dictionaries and lists. Enough for Grafana's provisioning
    // schema; avoids a YamlDotNet dependency.
    // -------------------------------------------------------------------------
    private static void AppendYaml(StringBuilder sb, IDictionary<string, object?> map, string indent)
    {
        foreach (var (key, value) in map)
            AppendYamlEntry(sb, $"{indent}{key}:", value, indent);
    }

    private static void AppendYamlEntry(StringBuilder sb, string prefix, object? value, string indent)
    {
        switch (value)
        {
            case IDictionary<string, object?> nested:
                sb.AppendLine(prefix);
                AppendYaml(sb, nested, indent + "  ");
                break;
            case IEnumerable<object?> list when value is not string:
                sb.AppendLine(prefix);
                foreach (var item in list)
                {
                    if (item is IDictionary<string, object?> itemMap)
                    {
                        var first = true;
                        foreach (var (k, v) in itemMap)
                        {
                            var linePrefix = first ? $"{indent}  - {k}:" : $"{indent}    {k}:";
                            AppendYamlEntry(sb, linePrefix, v, indent + "    ");
                            first = false;
                        }
                    }
                    else
                    {
                        sb.AppendLine($"{indent}  - {YamlValue(item)}");
                    }
                }
                break;
            default:
                sb.AppendLine($"{prefix} {YamlValue(value)}");
                break;
        }
    }

    private static string YamlValue(object? value) => value switch
    {
        null => "null",
        bool b => b ? "true" : "false",
        string s => YamlScalar(s),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => YamlScalar(value.ToString() ?? ""),
    };

    /// <summary>Single-quotes a string scalar (safe for URLs, env-var refs, leading dashes …).</summary>
    private static string YamlScalar(string value) => $"'{value.Replace("'", "''")}'";

    private static string Indent(string text, string indent) =>
        string.Join("\n", text.Split('\n').Select(l => l.Length == 0 ? l : indent + l));
}
