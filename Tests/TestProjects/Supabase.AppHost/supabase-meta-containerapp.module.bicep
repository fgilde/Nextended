@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

resource supabase_meta 'Microsoft.App/containerApps@2025-07-01' = {
  name: 'supabase-meta'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
        allowInsecure: true
      }
    }
    environmentId: env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'supabase/postgres-meta:v0.95.2'
          name: 'supabase-meta'
          command: [
            '/bin/sh'
          ]
          args: [
            '-c'
            'echo "\$DB_WAIT_SCRIPT_BASE64" | base64 -d > /tmp/db-wait.sh && chmod +x /tmp/db-wait.sh && exec /tmp/db-wait.sh'
          ]
          env: [
            {
              name: 'PG_META_PORT'
              value: '8080'
            }
            {
              name: 'PG_META_DB_NAME'
              value: 'postgres'
            }
            {
              name: 'PG_META_DB_USER'
              value: 'supabase_admin'
            }
            {
              name: 'PG_META_DB_PASSWORD'
              value: 'postgres-insecure-dev-password'
            }
            {
              name: 'CRYPTO_KEY'
              value: 'your-encryption-key-32-chars-min!!'
            }
            {
              name: 'PG_META_DB_HOST'
              value: 'supabase-db'
            }
            {
              name: 'PG_META_DB_PORT'
              value: '5432'
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
              value: 'IyEvYmluL3NoCmVjaG8gIltNZXRhXSBXYWl0aW5nIGZvciBEQiBhdCAkREJfV0FJVF9IT1NUOiREQl9XQUlUX1BPUlQuLi4iClJFVFJZPTAKd2hpbGUgWyAkUkVUUlkgLWx0IDYwIF07IGRvCiAgICBpZiBuYyAteiAkREJfV0FJVF9IT1NUICREQl9XQUlUX1BPUlQgMj4vZGV2L251bGw7IHRoZW4KICAgICAgICBlY2hvICJbTWV0YV0gREIgcmVhZHkgKG5jKSEgV2FpdGluZyAxMHMgZm9yIGluaXQuLi4iCiAgICAgICAgc2xlZXAgMTAKICAgICAgICBleGVjIHNoIC1jICdjZCAvdXNyL3NyYy9hcHAgJiYgZXhlYyBub2RlIGRpc3Qvc2VydmVyL3NlcnZlci5qcycKICAgIGVsaWYgbm9kZSAtZSAiY29uc3Qgcz1yZXF1aXJlKCduZXQnKS5jb25uZWN0KHtob3N0OnByb2Nlc3MuZW52LkRCX1dBSVRfSE9TVCxwb3J0OnByb2Nlc3MuZW52LkRCX1dBSVRfUE9SVH0sKCk9PntzLmVuZCgpO3Byb2Nlc3MuZXhpdCgwKX0pO3Mub24oJ2Vycm9yJywoKT0+cHJvY2Vzcy5leGl0KDEpKTtzZXRUaW1lb3V0KCgpPT5wcm9jZXNzLmV4aXQoMSksMzAwMCkiIDI+L2Rldi9udWxsOyB0aGVuCiAgICAgICAgZWNobyAiW01ldGFdIERCIHJlYWR5IChub2RlKSEgV2FpdGluZyAxMHMgZm9yIGluaXQuLi4iCiAgICAgICAgc2xlZXAgMTAKICAgICAgICBleGVjIHNoIC1jICdjZCAvdXNyL3NyYy9hcHAgJiYgZXhlYyBub2RlIGRpc3Qvc2VydmVyL3NlcnZlci5qcycKICAgIGVsaWYgY3VybCAtc2YgLS1jb25uZWN0LXRpbWVvdXQgMyB0ZWxuZXQ6Ly8kREJfV0FJVF9IT1NUOiREQl9XQUlUX1BPUlQgPC9kZXYvbnVsbCAyPi9kZXYvbnVsbDsgdGhlbgogICAgICAgIGVjaG8gIltNZXRhXSBEQiByZWFkeSAoY3VybCkhIFdhaXRpbmcgMTBzIGZvciBpbml0Li4uIgogICAgICAgIHNsZWVwIDEwCiAgICAgICAgZXhlYyBzaCAtYyAnY2QgL3Vzci9zcmMvYXBwICYmIGV4ZWMgbm9kZSBkaXN0L3NlcnZlci9zZXJ2ZXIuanMnCiAgICBmaQogICAgUkVUUlk9JCgoUkVUUlkrMSkpCiAgICBlY2hvICJbTWV0YV0gREIgbm90IHJlYWR5IChhdHRlbXB0ICRSRVRSWS82MCkiCiAgICBzbGVlcCA1CmRvbmUKZWNobyAiW01ldGFdIEVSUk9SOiBEQiBub3QgcmVhZHkgYWZ0ZXIgNSBtaW51dGVzIgpleGl0IDEK'
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