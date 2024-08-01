using './main.bicep'

param cosmosAccount = {
  accountName: readEnvironmentVariable('COSMOS_ACCOUNT_NAME')
  database: {
    containers: [
      {
        name: readEnvironmentVariable('COSMOS_CONTAINER_NAME_UTILITIES')
        partitionKeys: {
          paths: ['/id']
          kind: 'Hash'
        }
        indexingPolicy: {
          indexingMode: 'consistent'
          includedPaths: [
            { path: '/*' }
          ]
          excludedPaths: [
            { path: '/_etag/?' }
          ]
        }
      }
    ]
    name: readEnvironmentVariable('COSMOS_DATABASE_NAME')
    options: {
      throughput: 1000
    }
  }
}

param containerApp = {
  containerAppName: readEnvironmentVariable('CONTAINER_APP_NAME')
  environmentName: readEnvironmentVariable('CONTAINER_APP_ENVIRONMENT_NAME')
  image: readEnvironmentVariable('CONTAINER_APP_IMAGE')
  revisionSuffix: readEnvironmentVariable('CONTAINER_APP_REVISION_SUFFIX', 'current')
  cpuCore: readEnvironmentVariable('CONTAINER_APP_CPU_CORE', '0.5')
  memorySize: readEnvironmentVariable('CONTAINER_APP_MEMORY_SIZE', '0.5')
  maxReplicas: int(readEnvironmentVariable('CONTAINER_APP_MAX_REPLICAS', '1'))
  minReplicas: int(readEnvironmentVariable('CONTAINER_APP_MIN_REPLICAS', '1'))
  targetPort: int(readEnvironmentVariable('CONTAINER_APP_TARGET_PORT', '5001'))
}
