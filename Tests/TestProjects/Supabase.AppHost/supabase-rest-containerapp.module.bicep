@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

resource supabase_rest 'Microsoft.App/containerApps@2025-07-01' = {
  name: 'supabase-rest'
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
          image: 'postgrest/postgrest:v14.6'
          name: 'supabase-rest'
          env: [
            {
              name: 'PGRST_DB_URI'
              value: 'postgres://authenticator:postgres-insecure-dev-password@supabase-db:5432/postgres'
            }
            {
              name: 'PGRST_DB_SCHEMAS'
              value: 'public,storage,graphql_public'
            }
            {
              name: 'PGRST_DB_ANON_ROLE'
              value: 'anon'
            }
            {
              name: 'PGRST_JWT_SECRET'
              value: 'super-secret-jwt-token-with-at-least-32-characters-long'
            }
            {
              name: 'PGRST_DB_USE_LEGACY_GUCS'
              value: 'false'
            }
            {
              name: 'PGRST_APP_SETTINGS_JWT_SECRET'
              value: 'super-secret-jwt-token-with-at-least-32-characters-long'
            }
            {
              name: 'PGRST_APP_SETTINGS_JWT_EXP'
              value: '3600'
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