using System.Globalization;

namespace Nextended.Aspire.Hosting.Observability;

/// <summary>
/// Generates the YAML config files for the observability stack from runtime
/// values (hostnames, ports, options). Replaces the previously-static files
/// under <c>aspire/observability/</c> so the stack is fully parameterisable —
/// any consumer of the future NuGet package can change resource names without
/// editing files in the repo.
///
/// Each <c>Write*</c> method takes a destination path and a small bundle of
/// parameters. The generators are deliberately simple string interpolation —
/// no template engine, no dependency. They're meant to be readable side-by-side
/// with the YAML they produce.
/// </summary>
internal static class ObservabilityConfigGenerator
{
    // -------------------------------------------------------------------------
    // Hostnames bundle — one place to thread all resource names through, so
    // adding a new component later doesn't mean editing every generator.
    // -------------------------------------------------------------------------
    public sealed record Hostnames(
        string Prometheus,
        string Loki,
        string Tempo,
        string OtelCollector,
        string PostgresExporter,
        string CAdvisor,
        string PostgresDb,
        string PostgresPassword,
        string AspireDashboardOtlpEndpoint);

    // -------------------------------------------------------------------------
    // Prometheus
    // -------------------------------------------------------------------------
    public static string GetPrometheusYaml(Hostnames hosts, string retention)
    {
        var jobs = new List<string>
        {
            $$"""
              - job_name: prometheus
                static_configs:
                  - targets: ['localhost:9090']
            """,
        };

        // Scrape postgres_exporter only if it's part of this deployment.
        if (!string.IsNullOrEmpty(hosts.PostgresExporter))
        {
            jobs.Add($$"""
              - job_name: postgres
                static_configs:
                  - targets: ['{{hosts.PostgresExporter}}:9187']
            """);
        }

        // cAdvisor optional too.
        if (!string.IsNullOrEmpty(hosts.CAdvisor))
        {
            jobs.Add($$"""
              - job_name: cadvisor
                static_configs:
                  - targets: ['{{hosts.CAdvisor}}:8080']
                metric_relabel_configs:
                  - source_labels: [__name__]
                    regex: 'container_(memory_failures_total|tasks_state)'
                    action: drop
            """);
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
    public static string GetPromtailConfigYaml(Hostnames hosts) => $$"""
        server:
          http_listen_port: 9080
          grpc_listen_port: 0
          log_level: warn

        positions:
          filename: /tmp/positions.yaml

        clients:
          - url: http://{{hosts.Loki}}:3100/loki/api/v1/push

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
    public static string GetTempoConfigYaml(Hostnames hosts) => $"""
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
            block_retention: 168h

        metrics_generator:
          registry:
            external_labels:
              source: tempo
          storage:
            path: /tmp/tempo/generator/wal
            remote_write:
              - url: http://{hosts.Prometheus}:9090/api/v1/write
                send_exemplars: true
          traces_storage:
            path: /tmp/tempo/generator/traces

        storage:
          trace:
            backend: local
            wal:
              path: /tmp/tempo/wal
            local:
              path: /tmp/tempo/blocks

        overrides:
          defaults:
            metrics_generator:
              processors: ['service-graphs', 'span-metrics']
        """;

    // -------------------------------------------------------------------------
    // OpenTelemetry Collector (central fan-out)
    // -------------------------------------------------------------------------
    public static string GetOtelCollectorConfigYaml(Hostnames hosts)
    {
        // Build the exporter list dynamically — only include exporters whose
        // target service is actually in the stack. Avoids "connection refused"
        // noise in the collector logs when a backend isn't deployed.
        var tracesExporters = new List<string> { "debug" };
        var tracesExporterDefs = new List<string>();

        if (!string.IsNullOrEmpty(hosts.Tempo))
        {
            tracesExporterDefs.Add($$"""
                  otlp/tempo:
                    endpoint: {{hosts.Tempo}}:4317
                    tls:
                      insecure: true
                """);
            tracesExporters.Add("otlp/tempo");
        }

        // Aspire dashboard mirror — only when the endpoint is supplied. Lets
        // traces show up in the built-in dashboard alongside Grafana.
        if (!string.IsNullOrEmpty(hosts.AspireDashboardOtlpEndpoint))
        {
            tracesExporterDefs.Add($$"""
                  otlp/aspire:
                    endpoint: {{hosts.AspireDashboardOtlpEndpoint}}
                    tls:
                      insecure: true
                """);
            tracesExporters.Add("otlp/aspire");
        }

        var logsExporters = new List<string> { "debug" };
        var logsExporterDefs = new List<string>();
        if (!string.IsNullOrEmpty(hosts.Loki))
        {
            logsExporterDefs.Add($$"""
                  otlphttp/loki:
                    endpoint: http://{{hosts.Loki}}:3100/otlp
                    tls:
                      insecure: true
                """);
            logsExporters.Add("otlphttp/loki");
        }

        var metricsExporters = new List<string> { "debug" };
        var metricsExporterDefs = new List<string>();
        if (!string.IsNullOrEmpty(hosts.Prometheus))
        {
            metricsExporterDefs.Add($$"""
                  prometheusremotewrite:
                    endpoint: http://{{hosts.Prometheus}}:9090/api/v1/write
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
            {{string.Join("\n", tracesExporterDefs.Concat(logsExporterDefs).Concat(metricsExporterDefs))}}
              debug:
                # `detailed` logs the resource attributes + span names for each
                # received batch — invaluable when Kong/anyone-else isn't actually
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
    public static string GetGrafanaDatasourcesYaml(Hostnames hosts)
    {
        var sources = new List<string>();

        if (!string.IsNullOrEmpty(hosts.Prometheus))
        {
            sources.Add($$"""
                  - name: Prometheus
                    uid: prometheus
                    type: prometheus
                    access: proxy
                    url: http://{{hosts.Prometheus}}:9090
                    isDefault: true
                    editable: false
                """);
        }

        if (!string.IsNullOrEmpty(hosts.Loki))
        {
            sources.Add($$"""
                  - name: Loki
                    uid: loki
                    type: loki
                    access: proxy
                    url: http://{{hosts.Loki}}:3100
                    editable: false
                """);
        }

        if (!string.IsNullOrEmpty(hosts.Tempo))
        {
            sources.Add($$"""
                  - name: Tempo
                    uid: tempo
                    type: tempo
                    access: proxy
                    url: http://{{hosts.Tempo}}:3200
                    jsonData:
                      tracesToLogsV2:
                        datasourceUid: loki
                        spanStartTimeShift: '-1h'
                        spanEndTimeShift: '1h'
                        filterByTraceID: false
                        filterBySpanID: false
                        tags:
                          - key: service.name
                            value: container
                      serviceMap:
                        datasourceUid: prometheus
                      nodeGraph:
                        enabled: true
                    editable: false
                """);
        }

        if (!string.IsNullOrEmpty(hosts.PostgresDb))
        {
            // We keep using env-var placeholders for the secret so the file is
            // safe to commit / write to disk; Grafana resolves them on load.
            sources.Add($$"""
                  - name: Postgres
                    uid: postgres
                    type: postgres
                    access: proxy
                    url: {{hosts.PostgresDb}}:5432
                    user: postgres
                    secureJsonData:
                      password: ${APP_DB_PASSWORD}
                    jsonData:
                      database: postgres
                      sslmode: disable
                      postgresVersion: 1500
                      timescaledb: false
                    editable: false
                """);
        }

        return $"""
            apiVersion: 1

            datasources:
            {string.Join("\n", sources)}
            """;
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
}
