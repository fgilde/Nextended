@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

resource supabase 'Microsoft.App/containerApps@2025-07-01' = {
  name: 'supabase'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 3000
        transport: 'http'
        allowInsecure: true
      }
    }
    environmentId: env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'supabase/studio:2026.03.16-sha-5528817'
          name: 'supabase'
          env: [
            {
              name: 'STUDIO_PG_META_URL'
              value: 'https://supabase-meta.internal.${env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'POSTGRES_PASSWORD'
              value: 'postgres-insecure-dev-password'
            }
            {
              name: 'POSTGRES_DB'
              value: 'postgres'
            }
            {
              name: 'POSTGRES_USER'
              value: 'supabase_admin'
            }
            {
              name: 'DEFAULT_ORGANIZATION_NAME'
              value: 'Default Organization'
            }
            {
              name: 'DEFAULT_PROJECT_NAME'
              value: 'Default Project'
            }
            {
              name: 'SUPABASE_URL'
              value: 'https://supabase-kong.${env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'SUPABASE_PUBLIC_URL'
              value: 'https://supabase-kong.${env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'SUPABASE_ANON_KEY'
              value: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0'
            }
            {
              name: 'SUPABASE_SERVICE_KEY'
              value: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImV4cCI6MTk4MzgxMjk5Nn0.EGIM96RAZx35lJzdJsyH-qQwv8Hdp7fsn3W0YpN81IU'
            }
            {
              name: 'GOTRUE_URL'
              value: 'https://supabase-auth.internal.${env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'AUTH_JWT_SECRET'
              value: 'super-secret-jwt-token-with-at-least-32-characters-long'
            }
            {
              name: 'PG_META_CRYPTO_KEY'
              value: 'your-encryption-key-32-chars-min!!'
            }
            {
              name: 'LOGFLARE_API_KEY'
              value: ''
            }
            {
              name: 'LOGFLARE_URL'
              value: ''
            }
            {
              name: 'NEXT_PUBLIC_ENABLE_LOGS'
              value: 'false'
            }
            {
              name: 'NEXT_ANALYTICS_BACKEND_PROVIDER'
              value: ''
            }
            {
              name: 'SNIPPETS_MANAGEMENT_FOLDER'
              value: '/app/snippets'
            }
            {
              name: 'EDGE_FUNCTIONS_MANAGEMENT_FOLDER'
              value: '/app/edge-functions'
            }
            {
              name: 'POSTGRES_HOST'
              value: 'supabase-db'
            }
            {
              name: 'POSTGRES_PORT'
              value: '5432'
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