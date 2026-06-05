@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param env_outputs_azure_container_apps_environment_default_domain string

param env_outputs_azure_container_apps_environment_id string

resource supabase_kong 'Microsoft.App/containerApps@2025-07-01' = {
  name: 'supabase-kong'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8000
        transport: 'http'
        allowInsecure: true
      }
    }
    environmentId: env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'kong/kong:3.9.1'
          name: 'supabase-kong'
          command: [
            '/bin/sh'
          ]
          args: [
            '-c'
            'set -e && echo \'[Kong Init] Starting configuration...\' && echo \'[Kong Init] Decoding config template...\' && echo "\$KONG_CONFIG_TEMPLATE_BASE64" | base64 -d > /tmp/kong.yml.template && echo \'[Kong Init] Substituting URLs using sed...\' && sed -e "s|\\\${AUTH_URL}|\$AUTH_URL|g" -e "s|\\\${REST_URL}|\$REST_URL|g" -e "s|\\\${STORAGE_URL}|\$STORAGE_URL|g" -e "s|\\\${META_URL}|\$META_URL|g" -e "s|\\\${EDGE_URL}|\$EDGE_URL|g" -e "s|\\\${REALTIME_URL}|\$REALTIME_URL|g" /tmp/kong.yml.template > /tmp/kong.yml && echo \'[Kong Init] Config created. URLs:\' && echo "AUTH=\$AUTH_URL REST=\$REST_URL STORAGE=\$STORAGE_URL META=\$META_URL REALTIME=\$REALTIME_URL" && echo \'[Kong Init] Starting Kong...\' && export KONG_DECLARATIVE_CONFIG=/tmp/kong.yml && exec /entrypoint.sh kong docker-start'
          ]
          env: [
            {
              name: 'KONG_DATABASE'
              value: 'off'
            }
            {
              name: 'KONG_DNS_ORDER'
              value: 'LAST,A,CNAME'
            }
            {
              name: 'KONG_PLUGINS'
              value: 'request-transformer,cors,key-auth,acl,basic-auth'
            }
            {
              name: 'KONG_NGINX_PROXY_PROXY_BUFFER_SIZE'
              value: '160k'
            }
            {
              name: 'KONG_NGINX_PROXY_PROXY_BUFFERS'
              value: '64 160k'
            }
            {
              name: 'KONG_NGINX_PROXY_LARGE_CLIENT_HEADER_BUFFERS'
              value: '4 64k'
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
              name: 'KONG_CONFIG_TEMPLATE_BASE64'
              value: 'X2Zvcm1hdF92ZXJzaW9uOiAnMi4xJw0KX3RyYW5zZm9ybTogdHJ1ZQ0KDQpjb25zdW1lcnM6DQogIC0gdXNlcm5hbWU6IGFub24NCiAgICBrZXlhdXRoX2NyZWRlbnRpYWxzOg0KICAgICAgLSBrZXk6IGV5SmhiR2NpT2lKSVV6STFOaUlzSW5SNWNDSTZJa3BYVkNKOS5leUpwYzNNaU9pSnpkWEJoWW1GelpTMWtaVzF2SWl3aWNtOXNaU0k2SW1GdWIyNGlMQ0psZUhBaU9qRTVPRE00TVRJNU9UWjkuQ1JYUDFBN1dPZW9KZVh4ak5uaTQza2RRd2duV05SZWlsRE1ibFlUbl9JMA0KICAtIHVzZXJuYW1lOiBzZXJ2aWNlX3JvbGUNCiAgICBrZXlhdXRoX2NyZWRlbnRpYWxzOg0KICAgICAgLSBrZXk6IGV5SmhiR2NpT2lKSVV6STFOaUlzSW5SNWNDSTZJa3BYVkNKOS5leUpwYzNNaU9pSnpkWEJoWW1GelpTMWtaVzF2SWl3aWNtOXNaU0k2SW5ObGNuWnBZMlZmY205c1pTSXNJbVY0Y0NJNk1UazRNemd4TWprNU5uMC5FR0lNOTZSQVp4MzVsSnpkSnN5SC1xUXd2OEhkcDdmc24zVzBZcE44MUlVDQoNCmFjbHM6DQogIC0gY29uc3VtZXI6IGFub24NCiAgICBncm91cDogYW5vbg0KICAtIGNvbnN1bWVyOiBzZXJ2aWNlX3JvbGUNCiAgICBncm91cDogYWRtaW4NCg0Kc2VydmljZXM6DQogIC0gbmFtZTogYXV0aC12MS1vcGVuDQogICAgdXJsOiAke0FVVEhfVVJMfS92ZXJpZnkNCiAgICByb3V0ZXM6DQogICAgICAtIG5hbWU6IGF1dGgtdjEtb3Blbg0KICAgICAgICBzdHJpcF9wYXRoOiB0cnVlDQogICAgICAgIHBhdGhzOg0KICAgICAgICAgIC0gL2F1dGgvdjEvdmVyaWZ5DQogICAgcGx1Z2luczoNCiAgICAgIC0gbmFtZTogY29ycw0KDQogIC0gbmFtZTogYXV0aC12MS1vcGVuLWNhbGxiYWNrDQogICAgdXJsOiAke0FVVEhfVVJMfS9jYWxsYmFjaw0KICAgIHJvdXRlczoNCiAgICAgIC0gbmFtZTogYXV0aC12MS1vcGVuLWNhbGxiYWNrDQogICAgICAgIHN0cmlwX3BhdGg6IHRydWUNCiAgICAgICAgcGF0aHM6DQogICAgICAgICAgLSAvYXV0aC92MS9jYWxsYmFjaw0KICAgIHBsdWdpbnM6DQogICAgICAtIG5hbWU6IGNvcnMNCg0KICAtIG5hbWU6IGF1dGgtdjEtb3Blbi1hdXRob3JpemUNCiAgICB1cmw6ICR7QVVUSF9VUkx9L2F1dGhvcml6ZQ0KICAgIHJvdXRlczoNCiAgICAgIC0gbmFtZTogYXV0aC12MS1vcGVuLWF1dGhvcml6ZQ0KICAgICAgICBzdHJpcF9wYXRoOiB0cnVlDQogICAgICAgIHBhdGhzOg0KICAgICAgICAgIC0gL2F1dGgvdjEvYXV0aG9yaXplDQogICAgcGx1Z2luczoNCiAgICAgIC0gbmFtZTogY29ycw0KDQogIC0gbmFtZTogYXV0aC12MQ0KICAgIHVybDogJHtBVVRIX1VSTH0NCiAgICByb3V0ZXM6DQogICAgICAtIG5hbWU6IGF1dGgtdjENCiAgICAgICAgc3RyaXBfcGF0aDogdHJ1ZQ0KICAgICAgICBwYXRoczoNCiAgICAgICAgICAtIC9hdXRoL3YxLw0KICAgIHBsdWdpbnM6DQogICAgICAtIG5hbWU6IGNvcnMNCiAgICAgIC0gbmFtZToga2V5LWF1dGgNCiAgICAgICAgY29uZmlnOg0KICAgICAgICAgIGhpZGVfY3JlZGVudGlhbHM6IGZhbHNlDQogICAgICAtIG5hbWU6IGFjbA0KICAgICAgICBjb25maWc6DQogICAgICAgICAgaGlkZV9ncm91cHNfaGVhZGVyOiB0cnVlDQogICAgICAgICAgYWxsb3c6DQogICAgICAgICAgICAtIGFkbWluDQogICAgICAgICAgICAtIGFub24NCg0KICAtIG5hbWU6IHJlc3QtdjENCiAgICB1cmw6ICR7UkVTVF9VUkx9DQogICAgcm91dGVzOg0KICAgICAgLSBuYW1lOiByZXN0LXYxDQogICAgICAgIHN0cmlwX3BhdGg6IHRydWUNCiAgICAgICAgcGF0aHM6DQogICAgICAgICAgLSAvcmVzdC92MS8NCiAgICBwbHVnaW5zOg0KICAgICAgLSBuYW1lOiBjb3JzDQogICAgICAtIG5hbWU6IGtleS1hdXRoDQogICAgICAgIGNvbmZpZzoNCiAgICAgICAgICBoaWRlX2NyZWRlbnRpYWxzOiBmYWxzZQ0KICAgICAgLSBuYW1lOiBhY2wNCiAgICAgICAgY29uZmlnOg0KICAgICAgICAgIGhpZGVfZ3JvdXBzX2hlYWRlcjogdHJ1ZQ0KICAgICAgICAgIGFsbG93Og0KICAgICAgICAgICAgLSBhZG1pbg0KICAgICAgICAgICAgLSBhbm9uDQoNCiAgLSBuYW1lOiBzdG9yYWdlLXYxDQogICAgX2NvbW1lbnQ6IHwNCiAgICAgIFN0b3JhZ2UgaGFuZGxlcyBpdHMgb3duIGF1dGguIFB1dHRpbmcgS29uZydzIGtleS1hdXRoICsgYWNsIGluIGZyb250IG9mIHRoZSBTdG9yYWdlDQogICAgICBzZXJ2aWNlIGNhdXNlcyAvc3RvcmFnZS92MS9vYmplY3QvcHVibGljLzxidWNrZXQ+LzxwYXRoPiB0byByZXR1cm4gNDAxIChubyBhcGlrZXkNCiAgICAgIGhlYWRlciBmcm9tIDxpbWc+IHRhZ3MpLCB3aGljaCBzaWxlbnRseSBicmVha3MgZXZlcnkgYXZhdGFyIC8gYXBwLWxvZ28gLyBwdWJsaWMgYXNzZXQuDQogICAgICBUaGUgU3RvcmFnZSBBUEkgaXRzZWxmIGNoZWNrcyBKV1QgZm9yIGF1dGhlbnRpY2F0ZWQgcm91dGVzIGFuZCBsZXRzIHB1YmxpYyBidWNrZXRzDQogICAgICB0aHJvdWdoIHdpdGhvdXQgYXV0aCwgc28gS29uZyBvbmx5IG5lZWRzIGNvcnMgaGVyZS4NCiAgICB1cmw6ICR7U1RPUkFHRV9VUkx9DQogICAgcm91dGVzOg0KICAgICAgLSBuYW1lOiBzdG9yYWdlLXYxDQogICAgICAgIHN0cmlwX3BhdGg6IHRydWUNCiAgICAgICAgcGF0aHM6DQogICAgICAgICAgLSAvc3RvcmFnZS92MS8NCiAgICBwbHVnaW5zOg0KICAgICAgLSBuYW1lOiBjb3JzDQoNCiAgLSBuYW1lOiBtZXRhDQogICAgdXJsOiAke01FVEFfVVJMfQ0KICAgIHJvdXRlczoNCiAgICAgIC0gbmFtZTogbWV0YQ0KICAgICAgICBzdHJpcF9wYXRoOiB0cnVlDQogICAgICAgIHBhdGhzOg0KICAgICAgICAgIC0gL3BnLw0KICAgIHBsdWdpbnM6DQogICAgICAtIG5hbWU6IGtleS1hdXRoDQogICAgICAgIGNvbmZpZzoNCiAgICAgICAgICBoaWRlX2NyZWRlbnRpYWxzOiBmYWxzZQ0KICAgICAgLSBuYW1lOiBhY2wNCiAgICAgICAgY29uZmlnOg0KICAgICAgICAgIGhpZGVfZ3JvdXBzX2hlYWRlcjogdHJ1ZQ0KICAgICAgICAgIGFsbG93Og0KICAgICAgICAgICAgLSBhZG1pbg0KDQogIC0gbmFtZTogcmVhbHRpbWUtdjEtd3MNCiAgICBfY29tbWVudDogIlJlYWx0aW1lOiAvcmVhbHRpbWUvdjEvKiAtPiB3czovL3JlYWx0aW1lOjQwMDAvc29ja2V0LyoiDQogICAgdXJsOiAke1JFQUxUSU1FX1VSTH0vc29ja2V0DQogICAgcm91dGVzOg0KICAgICAgLSBuYW1lOiByZWFsdGltZS12MS13cw0KICAgICAgICBzdHJpcF9wYXRoOiB0cnVlDQogICAgICAgIHBhdGhzOg0KICAgICAgICAgIC0gL3JlYWx0aW1lL3YxLw0KICAgIHBsdWdpbnM6DQogICAgICAtIG5hbWU6IGNvcnMNCiAgICAgIC0gbmFtZToga2V5LWF1dGgNCiAgICAgICAgY29uZmlnOg0KICAgICAgICAgIGhpZGVfY3JlZGVudGlhbHM6IGZhbHNlDQogICAgICAtIG5hbWU6IGFjbA0KICAgICAgICBjb25maWc6DQogICAgICAgICAgaGlkZV9ncm91cHNfaGVhZGVyOiB0cnVlDQogICAgICAgICAgYWxsb3c6DQogICAgICAgICAgICAtIGFkbWluDQogICAgICAgICAgICAtIGFub24NCiAgICAgIC0gbmFtZTogcmVxdWVzdC10cmFuc2Zvcm1lcg0KICAgICAgICBjb25maWc6DQogICAgICAgICAgcmVtb3ZlOg0KICAgICAgICAgICAgaGVhZGVyczoNCiAgICAgICAgICAgICAgLSBjb29raWUNCiAgICAgICAgICByZXBsYWNlOg0KICAgICAgICAgICAgaGVhZGVyczoNCiAgICAgICAgICAgICAgLSAiSG9zdDogcmVhbHRpbWUtZGV2LnN1cGFiYXNlLXJlYWx0aW1lIg0KDQogIC0gbmFtZTogcmVhbHRpbWUtdjEtcmVzdA0KICAgIF9jb21tZW50OiAiUmVhbHRpbWUgUkVTVCBBUEk6IC9yZWFsdGltZS92MS9hcGkvKiAtPiBodHRwOi8vcmVhbHRpbWU6NDAwMC9hcGkvKiINCiAgICB1cmw6ICR7UkVBTFRJTUVfVVJMfS9hcGkNCiAgICByb3V0ZXM6DQogICAgICAtIG5hbWU6IHJlYWx0aW1lLXYxLXJlc3QNCiAgICAgICAgc3RyaXBfcGF0aDogdHJ1ZQ0KICAgICAgICBwYXRoczoNCiAgICAgICAgICAtIC9yZWFsdGltZS92MS9hcGkNCiAgICBwbHVnaW5zOg0KICAgICAgLSBuYW1lOiBjb3JzDQogICAgICAtIG5hbWU6IGtleS1hdXRoDQogICAgICAgIGNvbmZpZzoNCiAgICAgICAgICBoaWRlX2NyZWRlbnRpYWxzOiBmYWxzZQ0KICAgICAgLSBuYW1lOiBhY2wNCiAgICAgICAgY29uZmlnOg0KICAgICAgICAgIGhpZGVfZ3JvdXBzX2hlYWRlcjogdHJ1ZQ0KICAgICAgICAgIGFsbG93Og0KICAgICAgICAgICAgLSBhZG1pbg0KICAgICAgICAgICAgLSBhbm9uDQogICAgICAtIG5hbWU6IHJlcXVlc3QtdHJhbnNmb3JtZXINCiAgICAgICAgY29uZmlnOg0KICAgICAgICAgIHJlbW92ZToNCiAgICAgICAgICAgIGhlYWRlcnM6DQogICAgICAgICAgICAgIC0gY29va2llDQogICAgICAgICAgcmVwbGFjZToNCiAgICAgICAgICAgIGhlYWRlcnM6DQogICAgICAgICAgICAgIC0gIkhvc3Q6IHJlYWx0aW1lLWRldi5zdXBhYmFzZS1yZWFsdGltZSINCg0KICAtIG5hbWU6IGZ1bmN0aW9ucy12MQ0KICAgIHVybDogJHtFREdFX1VSTH0NCiAgICByb3V0ZXM6DQogICAgICAtIG5hbWU6IGZ1bmN0aW9ucy12MQ0KICAgICAgICBzdHJpcF9wYXRoOiBmYWxzZQ0KICAgICAgICBwYXRoczoNCiAgICAgICAgICAtIC9mdW5jdGlvbnMvdjEvDQogICAgcGx1Z2luczoNCiAgICAgIC0gbmFtZTogY29ycw0KICAgICAgLSBuYW1lOiBrZXktYXV0aA0KICAgICAgICBjb25maWc6DQogICAgICAgICAgaGlkZV9jcmVkZW50aWFsczogZmFsc2UNCiAgICAgIC0gbmFtZTogYWNsDQogICAgICAgIGNvbmZpZzoNCiAgICAgICAgICBoaWRlX2dyb3Vwc19oZWFkZXI6IHRydWUNCiAgICAgICAgICBhbGxvdzoNCiAgICAgICAgICAgIC0gYWRtaW4NCiAgICAgICAgICAgIC0gYW5vbg=='
            }
            {
              name: 'AUTH_URL'
              value: 'https://supabase-auth.internal.${env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'REST_URL'
              value: 'https://supabase-rest.internal.${env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'STORAGE_URL'
              value: 'https://supabase-storage.internal.${env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'META_URL'
              value: 'https://supabase-meta.internal.${env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'REALTIME_URL'
              value: 'https://supabase-realtime.internal.${env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'EDGE_URL'
              value: 'http://supabase-edge:9000'
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