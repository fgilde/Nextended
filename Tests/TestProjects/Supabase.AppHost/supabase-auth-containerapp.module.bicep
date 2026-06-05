@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

resource supabase_auth 'Microsoft.App/containerApps@2025-07-01' = {
  name: 'supabase-auth'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 9999
        transport: 'http'
        allowInsecure: true
      }
    }
    environmentId: env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'supabase/gotrue:v2.186.0'
          name: 'supabase-auth'
          command: [
            '/bin/sh'
          ]
          args: [
            '-c'
            'echo "\$DB_WAIT_SCRIPT_BASE64" | base64 -d > /tmp/db-wait.sh && chmod +x /tmp/db-wait.sh && exec /tmp/db-wait.sh'
          ]
          env: [
            {
              name: 'GOTRUE_API_HOST'
              value: '0.0.0.0'
            }
            {
              name: 'GOTRUE_API_PORT'
              value: '9999'
            }
            {
              name: 'GOTRUE_DB_DRIVER'
              value: 'postgres'
            }
            {
              name: 'GOTRUE_DB_DATABASE_URL'
              value: 'postgres://supabase_auth_admin:postgres-insecure-dev-password@supabase-db:5432/postgres?search_path=auth'
            }
            {
              name: 'GOTRUE_DB_NAMESPACE'
              value: 'auth'
            }
            {
              name: 'GOTRUE_SITE_URL'
              value: 'http://localhost:3000'
            }
            {
              name: 'GOTRUE_URI_ALLOW_LIST'
              value: '*'
            }
            {
              name: 'GOTRUE_JWT_SECRET'
              value: 'super-secret-jwt-token-with-at-least-32-characters-long'
            }
            {
              name: 'GOTRUE_JWT_EXP'
              value: '3600'
            }
            {
              name: 'GOTRUE_JWT_DEFAULT_GROUP_NAME'
              value: 'authenticated'
            }
            {
              name: 'GOTRUE_JWT_ADMIN_ROLES'
              value: 'service_role'
            }
            {
              name: 'GOTRUE_JWT_AUD'
              value: 'authenticated'
            }
            {
              name: 'GOTRUE_EXTERNAL_EMAIL_ENABLED'
              value: 'true'
            }
            {
              name: 'GOTRUE_MAILER_AUTOCONFIRM'
              value: 'true'
            }
            {
              name: 'GOTRUE_MAILER_SECURE_EMAIL_CHANGE_ENABLED'
              value: 'false'
            }
            {
              name: 'GOTRUE_DISABLE_SIGNUP'
              value: 'false'
            }
            {
              name: 'GOTRUE_ANONYMOUS_USERS_ENABLED'
              value: 'true'
            }
            {
              name: 'GOTRUE_RATE_LIMIT_HEADER'
              value: 'X-Forwarded-For'
            }
            {
              name: 'GOTRUE_RATE_LIMIT_EMAIL_SENT'
              value: '100'
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
              value: 'IyEvYmluL3NoCmVjaG8gIltBdXRoXSBXYWl0aW5nIGZvciBEQiBhdCAkREJfV0FJVF9IT1NUOiREQl9XQUlUX1BPUlQuLi4iClJFVFJZPTAKd2hpbGUgWyAkUkVUUlkgLWx0IDYwIF07IGRvCiAgICBpZiBuYyAteiAkREJfV0FJVF9IT1NUICREQl9XQUlUX1BPUlQgMj4vZGV2L251bGw7IHRoZW4KICAgICAgICBlY2hvICJbQXV0aF0gREIgcmVhZHkgKG5jKSEgV2FpdGluZyAxMHMgZm9yIGluaXQuLi4iCiAgICAgICAgc2xlZXAgMTAKICAgICAgICBleGVjIGF1dGgKICAgIGVsaWYgbm9kZSAtZSAiY29uc3Qgcz1yZXF1aXJlKCduZXQnKS5jb25uZWN0KHtob3N0OnByb2Nlc3MuZW52LkRCX1dBSVRfSE9TVCxwb3J0OnByb2Nlc3MuZW52LkRCX1dBSVRfUE9SVH0sKCk9PntzLmVuZCgpO3Byb2Nlc3MuZXhpdCgwKX0pO3Mub24oJ2Vycm9yJywoKT0+cHJvY2Vzcy5leGl0KDEpKTtzZXRUaW1lb3V0KCgpPT5wcm9jZXNzLmV4aXQoMSksMzAwMCkiIDI+L2Rldi9udWxsOyB0aGVuCiAgICAgICAgZWNobyAiW0F1dGhdIERCIHJlYWR5IChub2RlKSEgV2FpdGluZyAxMHMgZm9yIGluaXQuLi4iCiAgICAgICAgc2xlZXAgMTAKICAgICAgICBleGVjIGF1dGgKICAgIGVsaWYgY3VybCAtc2YgLS1jb25uZWN0LXRpbWVvdXQgMyB0ZWxuZXQ6Ly8kREJfV0FJVF9IT1NUOiREQl9XQUlUX1BPUlQgPC9kZXYvbnVsbCAyPi9kZXYvbnVsbDsgdGhlbgogICAgICAgIGVjaG8gIltBdXRoXSBEQiByZWFkeSAoY3VybCkhIFdhaXRpbmcgMTBzIGZvciBpbml0Li4uIgogICAgICAgIHNsZWVwIDEwCiAgICAgICAgZXhlYyBhdXRoCiAgICBmaQogICAgUkVUUlk9JCgoUkVUUlkrMSkpCiAgICBlY2hvICJbQXV0aF0gREIgbm90IHJlYWR5IChhdHRlbXB0ICRSRVRSWS82MCkiCiAgICBzbGVlcCA1CmRvbmUKZWNobyAiW0F1dGhdIEVSUk9SOiBEQiBub3QgcmVhZHkgYWZ0ZXIgNSBtaW51dGVzIgpleGl0IDEK'
            }
            {
              name: 'API_EXTERNAL_URL'
              value: 'https://supabase-kong.${env_outputs_azure_container_apps_environment_default_domain}'
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