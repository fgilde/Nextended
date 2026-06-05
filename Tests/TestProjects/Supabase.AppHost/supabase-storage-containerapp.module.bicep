@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

param env_outputs_volumes_supabase_storage_0 string

resource supabase_storage 'Microsoft.App/containerApps@2025-07-01' = {
  name: 'supabase-storage'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 5000
        transport: 'http'
        allowInsecure: true
      }
    }
    environmentId: env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'supabase/storage-api:v1.44.2'
          name: 'supabase-storage'
          command: [
            '/bin/sh'
          ]
          args: [
            '-c'
            'echo "\$DB_WAIT_SCRIPT_BASE64" | base64 -d > /tmp/db-wait.sh && chmod +x /tmp/db-wait.sh && exec /tmp/db-wait.sh'
          ]
          env: [
            {
              name: 'ANON_KEY'
              value: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0'
            }
            {
              name: 'SERVICE_KEY'
              value: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImV4cCI6MTk4MzgxMjk5Nn0.EGIM96RAZx35lJzdJsyH-qQwv8Hdp7fsn3W0YpN81IU'
            }
            {
              name: 'POSTGREST_URL'
              value: 'https://supabase-rest.internal.${env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'PGRST_JWT_SECRET'
              value: 'super-secret-jwt-token-with-at-least-32-characters-long'
            }
            {
              name: 'DATABASE_URL'
              value: 'postgres://supabase_storage_admin:postgres-insecure-dev-password@supabase-db:5432/postgres'
            }
            {
              name: 'FILE_STORAGE_BACKEND_PATH'
              value: '/var/lib/storage'
            }
            {
              name: 'STORAGE_BACKEND'
              value: 'file'
            }
            {
              name: 'FILE_SIZE_LIMIT'
              value: '52428800'
            }
            {
              name: 'TENANT_ID'
              value: 'stub'
            }
            {
              name: 'REGION'
              value: 'azure'
            }
            {
              name: 'GLOBAL_S3_BUCKET'
              value: 'stub'
            }
            {
              name: 'IS_MULTITENANT'
              value: 'false'
            }
            {
              name: 'ENABLE_IMAGE_TRANSFORMATION'
              value: 'true'
            }
            {
              name: 'REQUEST_ALLOW_X_FORWARDED_PATH'
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
              value: 'IyEvYmluL3NoCmVjaG8gIltTdG9yYWdlXSBXYWl0aW5nIGZvciBEQiBhdCAkREJfV0FJVF9IT1NUOiREQl9XQUlUX1BPUlQuLi4iClJFVFJZPTAKd2hpbGUgWyAkUkVUUlkgLWx0IDYwIF07IGRvCiAgICBpZiBuYyAteiAkREJfV0FJVF9IT1NUICREQl9XQUlUX1BPUlQgMj4vZGV2L251bGw7IHRoZW4KICAgICAgICBlY2hvICJbU3RvcmFnZV0gREIgcmVhZHkgKG5jKSEgV2FpdGluZyAxMHMgZm9yIGluaXQuLi4iCiAgICAgICAgc2xlZXAgMTAKICAgICAgICBleGVjIGRvY2tlci1lbnRyeXBvaW50LnNoIG5vZGUgZGlzdC9zdGFydC9zZXJ2ZXIuanMKICAgIGVsaWYgbm9kZSAtZSAiY29uc3Qgcz1yZXF1aXJlKCduZXQnKS5jb25uZWN0KHtob3N0OnByb2Nlc3MuZW52LkRCX1dBSVRfSE9TVCxwb3J0OnByb2Nlc3MuZW52LkRCX1dBSVRfUE9SVH0sKCk9PntzLmVuZCgpO3Byb2Nlc3MuZXhpdCgwKX0pO3Mub24oJ2Vycm9yJywoKT0+cHJvY2Vzcy5leGl0KDEpKTtzZXRUaW1lb3V0KCgpPT5wcm9jZXNzLmV4aXQoMSksMzAwMCkiIDI+L2Rldi9udWxsOyB0aGVuCiAgICAgICAgZWNobyAiW1N0b3JhZ2VdIERCIHJlYWR5IChub2RlKSEgV2FpdGluZyAxMHMgZm9yIGluaXQuLi4iCiAgICAgICAgc2xlZXAgMTAKICAgICAgICBleGVjIGRvY2tlci1lbnRyeXBvaW50LnNoIG5vZGUgZGlzdC9zdGFydC9zZXJ2ZXIuanMKICAgIGVsaWYgY3VybCAtc2YgLS1jb25uZWN0LXRpbWVvdXQgMyB0ZWxuZXQ6Ly8kREJfV0FJVF9IT1NUOiREQl9XQUlUX1BPUlQgPC9kZXYvbnVsbCAyPi9kZXYvbnVsbDsgdGhlbgogICAgICAgIGVjaG8gIltTdG9yYWdlXSBEQiByZWFkeSAoY3VybCkhIFdhaXRpbmcgMTBzIGZvciBpbml0Li4uIgogICAgICAgIHNsZWVwIDEwCiAgICAgICAgZXhlYyBkb2NrZXItZW50cnlwb2ludC5zaCBub2RlIGRpc3Qvc3RhcnQvc2VydmVyLmpzCiAgICBmaQogICAgUkVUUlk9JCgoUkVUUlkrMSkpCiAgICBlY2hvICJbU3RvcmFnZV0gREIgbm90IHJlYWR5IChhdHRlbXB0ICRSRVRSWS82MCkiCiAgICBzbGVlcCA1CmRvbmUKZWNobyAiW1N0b3JhZ2VdIEVSUk9SOiBEQiBub3QgcmVhZHkgYWZ0ZXIgNSBtaW51dGVzIgpleGl0IDEK'
            }
          ]
          volumeMounts: [
            {
              volumeName: 'v0'
              mountPath: '/var/lib/storage'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
      }
      volumes: [
        {
          name: 'v0'
          storageType: 'AzureFile'
          storageName: env_outputs_volumes_supabase_storage_0
        }
      ]
    }
  }
}