@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

resource supabase_realtime 'Microsoft.App/containerApps@2025-07-01' = {
  name: 'supabase-realtime'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 4000
        transport: 'http'
        allowInsecure: true
      }
    }
    environmentId: env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'supabase/realtime:v2.76.5'
          name: 'supabase-realtime'
          command: [
            '/bin/sh'
          ]
          args: [
            '-c'
            'echo "\$DB_WAIT_SCRIPT_BASE64" | base64 -d > /tmp/db-wait.sh && chmod +x /tmp/db-wait.sh && exec /tmp/db-wait.sh'
          ]
          env: [
            {
              name: 'PORT'
              value: '4000'
            }
            {
              name: 'DB_HOST'
              value: 'supabase-db'
            }
            {
              name: 'DB_PORT'
              value: '5432'
            }
            {
              name: 'DB_USER'
              value: 'supabase_admin'
            }
            {
              name: 'DB_PASSWORD'
              value: 'postgres-insecure-dev-password'
            }
            {
              name: 'DB_NAME'
              value: 'postgres'
            }
            {
              name: 'DB_AFTER_CONNECT_QUERY'
              value: 'SET search_path TO _realtime'
            }
            {
              name: 'DB_ENC_KEY'
              value: 'supabaserealtime'
            }
            {
              name: 'API_JWT_SECRET'
              value: 'super-secret-jwt-token-with-at-least-32-characters-long'
            }
            {
              name: 'SECRET_KEY_BASE'
              value: 'UpNVntn3cDxHJpq99YMc1T1AQgQpc8kfYTuRgBiYa15BLrx8etQoXz3gZv1/u2oq'
            }
            {
              name: 'ERL_AFLAGS'
              value: '-proto_dist inet_tcp'
            }
            {
              name: 'DNS_NODES'
              value: ''
            }
            {
              name: 'APP_NAME'
              value: 'realtime'
            }
            {
              name: 'SEED_SELF_HOST'
              value: 'true'
            }
            {
              name: 'RUN_JANITOR'
              value: 'true'
            }
            {
              name: 'RLIMIT_NOFILE'
              value: '10000'
            }
            {
              name: 'ENABLE_ERL_CRASH_DUMP'
              value: 'false'
            }
            {
              name: 'DISABLE_HEALTHCHECK_LOGGING'
              value: 'true'
            }
            {
              name: 'DB_WAIT_HOST'
              value: 'supabase-db'
            }
            {
              name: 'DB_WAIT_PORT'
              value: '5432'
            }
            {
              name: 'DB_WAIT_SCRIPT_BASE64'
              value: 'IyEvYmluL3NoCmVjaG8gIltSZWFsdGltZV0gV2FpdGluZyBmb3IgREIgYXQgJERCX1dBSVRfSE9TVDokREJfV0FJVF9QT1JULi4uIgpSRVRSWT0wCndoaWxlIFsgJFJFVFJZIC1sdCA2MCBdOyBkbwogICAgaWYgbmMgLXogJERCX1dBSVRfSE9TVCAkREJfV0FJVF9QT1JUIDI+L2Rldi9udWxsOyB0aGVuCiAgICAgICAgZWNobyAiW1JlYWx0aW1lXSBEQiByZWFkeSAobmMpISBXYWl0aW5nIDEwcyBmb3IgaW5pdC4uLiIKICAgICAgICBzbGVlcCAxMAogICAgICAgIGV4ZWMgL3Vzci9iaW4vdGluaSAtcyAtZyAtLSAvYXBwL3J1bi5zaCAvYXBwL2Jpbi9zZXJ2ZXIKICAgIGVsaWYgbm9kZSAtZSAiY29uc3Qgcz1yZXF1aXJlKCduZXQnKS5jb25uZWN0KHtob3N0OnByb2Nlc3MuZW52LkRCX1dBSVRfSE9TVCxwb3J0OnByb2Nlc3MuZW52LkRCX1dBSVRfUE9SVH0sKCk9PntzLmVuZCgpO3Byb2Nlc3MuZXhpdCgwKX0pO3Mub24oJ2Vycm9yJywoKT0+cHJvY2Vzcy5leGl0KDEpKTtzZXRUaW1lb3V0KCgpPT5wcm9jZXNzLmV4aXQoMSksMzAwMCkiIDI+L2Rldi9udWxsOyB0aGVuCiAgICAgICAgZWNobyAiW1JlYWx0aW1lXSBEQiByZWFkeSAobm9kZSkhIFdhaXRpbmcgMTBzIGZvciBpbml0Li4uIgogICAgICAgIHNsZWVwIDEwCiAgICAgICAgZXhlYyAvdXNyL2Jpbi90aW5pIC1zIC1nIC0tIC9hcHAvcnVuLnNoIC9hcHAvYmluL3NlcnZlcgogICAgZWxpZiBjdXJsIC1zZiAtLWNvbm5lY3QtdGltZW91dCAzIHRlbG5ldDovLyREQl9XQUlUX0hPU1Q6JERCX1dBSVRfUE9SVCA8L2Rldi9udWxsIDI+L2Rldi9udWxsOyB0aGVuCiAgICAgICAgZWNobyAiW1JlYWx0aW1lXSBEQiByZWFkeSAoY3VybCkhIFdhaXRpbmcgMTBzIGZvciBpbml0Li4uIgogICAgICAgIHNsZWVwIDEwCiAgICAgICAgZXhlYyAvdXNyL2Jpbi90aW5pIC1zIC1nIC0tIC9hcHAvcnVuLnNoIC9hcHAvYmluL3NlcnZlcgogICAgZmkKICAgIFJFVFJZPSQoKFJFVFJZKzEpKQogICAgZWNobyAiW1JlYWx0aW1lXSBEQiBub3QgcmVhZHkgKGF0dGVtcHQgJFJFVFJZLzYwKSIKICAgIHNsZWVwIDUKZG9uZQplY2hvICJbUmVhbHRpbWVdIEVSUk9SOiBEQiBub3QgcmVhZHkgYWZ0ZXIgNSBtaW51dGVzIgpleGl0IDEK'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
}