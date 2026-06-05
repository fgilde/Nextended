@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

@secure()
param n8n_redis_password_value string

resource n8n_redis 'Microsoft.App/containerApps@2025-07-01' = {
  name: 'n8n-redis'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'redis-password'
          value: n8n_redis_password_value
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 6379
        transport: 'tcp'
      }
    }
    environmentId: env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'docker.io/library/redis:8.6'
          name: 'n8n-redis'
          command: [
            '/bin/sh'
          ]
          args: [
            '-c'
            'redis-server --requirepass \$REDIS_PASSWORD'
          ]
          env: [
            {
              name: 'REDIS_PASSWORD'
              secretRef: 'redis-password'
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