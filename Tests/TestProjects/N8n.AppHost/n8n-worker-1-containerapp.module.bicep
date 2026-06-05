@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

@secure()
param n8n_pg_password_value string

@secure()
param n8n_redis_password_value string

resource n8n_worker_1 'Microsoft.App/containerApps@2025-07-01' = {
  name: 'n8n-worker-1'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'db-postgresdb-password'
          value: n8n_pg_password_value
        }
        {
          name: 'queue-bull-redis-password'
          value: n8n_redis_password_value
        }
      ]
      activeRevisionsMode: 'Single'
    }
    environmentId: env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'n8nio/n8n:1.110.1'
          name: 'n8n-worker-1'
          args: [
            'worker'
          ]
          env: [
            {
              name: 'N8N_PORT'
              value: '5678'
            }
            {
              name: 'N8N_PROTOCOL'
              value: 'https'
            }
            {
              name: 'N8N_HOST'
              value: 'n8n.${env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'N8N_ENCRYPTION_KEY'
              value: 'n8n-insecure-dev-encryption-key-change-me'
            }
            {
              name: 'GENERIC_TIMEZONE'
              value: 'Europe/Berlin'
            }
            {
              name: 'TZ'
              value: 'Europe/Berlin'
            }
            {
              name: 'N8N_DIAGNOSTICS_ENABLED'
              value: 'false'
            }
            {
              name: 'N8N_HIRING_BANNER_ENABLED'
              value: 'false'
            }
            {
              name: 'N8N_PERSONALIZATION_ENABLED'
              value: 'false'
            }
            {
              name: 'N8N_VERSION_NOTIFICATIONS_ENABLED'
              value: 'false'
            }
            {
              name: 'N8N_RUNNERS_ENABLED'
              value: 'true'
            }
            {
              name: 'N8N_SECURE_COOKIE'
              value: 'true'
            }
            {
              name: 'DB_TYPE'
              value: 'postgresdb'
            }
            {
              name: 'DB_POSTGRESDB_HOST'
              value: 'n8n-pg'
            }
            {
              name: 'DB_POSTGRESDB_PORT'
              value: '5432'
            }
            {
              name: 'DB_POSTGRESDB_DATABASE'
              value: 'n8n'
            }
            {
              name: 'DB_POSTGRESDB_USER'
              value: 'postgres'
            }
            {
              name: 'DB_POSTGRESDB_PASSWORD'
              secretRef: 'db-postgresdb-password'
            }
            {
              name: 'DB_POSTGRESDB_SCHEMA'
              value: 'public'
            }
            {
              name: 'N8N_BASIC_AUTH_ACTIVE'
              value: 'true'
            }
            {
              name: 'N8N_BASIC_AUTH_USER'
              value: 'admin'
            }
            {
              name: 'N8N_BASIC_AUTH_PASSWORD'
              value: 'n8n-dev-password'
            }
            {
              name: 'WEBHOOK_URL'
              value: 'https://n8n.${env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'N8N_EDITOR_BASE_URL'
              value: 'https://n8n.${env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'N8N_PROXY_HOPS'
              value: '1'
            }
            {
              name: 'EXECUTIONS_MODE'
              value: 'queue'
            }
            {
              name: 'QUEUE_BULL_REDIS_HOST'
              value: 'n8n-redis'
            }
            {
              name: 'QUEUE_BULL_REDIS_PORT'
              value: '6379'
            }
            {
              name: 'QUEUE_BULL_REDIS_PASSWORD'
              secretRef: 'queue-bull-redis-password'
            }
            {
              name: 'QUEUE_HEALTH_CHECK_ACTIVE'
              value: 'true'
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