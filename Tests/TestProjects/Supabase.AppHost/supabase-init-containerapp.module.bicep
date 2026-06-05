@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

resource supabase_init 'Microsoft.App/containerApps@2025-07-01' = {
  name: 'supabase-init'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
    }
    environmentId: env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'supabase/postgres:15.8.1.085'
          name: 'supabase-init'
          command: [
            '/bin/bash'
          ]
          args: [
            '-c'
            'set -e && echo \'[Post-Init] Waiting for Auth to initialize...\' && sleep 60 && echo "\$POST_INIT_SQL_GZ_BASE64" | base64 -d | gunzip > /tmp/post_init.sql && echo "[Post-Init] SQL size: \$(wc -c < /tmp/post_init.sql) bytes" && echo \'[Post-Init] Running post-init SQL (triggers, migrations, profiles)...\' && PGPASSWORD="\$DB_PASSWORD" psql -h "\$DB_HOST" -p "\$DB_PORT" -U supabase_admin -d postgres -v new_password="\$DB_PASSWORD" -f /tmp/post_init.sql && echo \'[Post-Init] Completed successfully\''
          ]
          env: [
            {
              name: 'POST_INIT_SQL_GZ_BASE64'
              value: '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'
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
              name: 'DB_PASSWORD'
              value: 'postgres-insecure-dev-password'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}