using './main.bicep'

param cosmosAccount = {
  accountName: readEnvironmentVariable('COSMOS_ACCOUNT_NAME', '')
  database: {
    containers: [
      {
        name: readEnvironmentVariable('COSMOS_CONTAINER_NAME_ELECTRICITY_USAGE', '')
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
      {
        name: readEnvironmentVariable('COSMOS_CONTAINER_NAME_ELECTRICITY_PURCHASES', '')
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
      {
        name: readEnvironmentVariable('COSMOS_CONTAINER_NAME_WATER_READING', '')
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
    name: readEnvironmentVariable('COSMOS_DATABASE_NAME', '')
    options: {
      throughput: 1000
    }
  }
}

param containerApp = {
  containerAppName: readEnvironmentVariable('CONTAINER_APP_NAME', '')
  environment: {
    name: readEnvironmentVariable('CONTAINER_APP_ENVIRONMENT_NAME', '')
    certificate: {
      name: readEnvironmentVariable('CONTAINER_APP_CERTIFICATE_NAME', '')
      subjectName: readEnvironmentVariable('CONTAINER_APP_CERTIFICATE_SUBJECT_NAME', '')
      domainControlValidation: readEnvironmentVariable('CONTAINER_APP_CERTIFICATE_DOMAIN_CONTROL_VALIDATION', '')
    }
  }
  image: readEnvironmentVariable('CONTAINER_APP_IMAGE')
  revisionSuffix: readEnvironmentVariable('CONTAINER_APP_REVISION_SUFFIX', 'current')
  cpuCore: readEnvironmentVariable('CONTAINER_APP_CPU_CORE', '0.5')
  memorySize: readEnvironmentVariable('CONTAINER_APP_MEMORY_SIZE', '1.0')
  maxReplicas: int(readEnvironmentVariable('CONTAINER_APP_MAX_REPLICAS', '1'))
  minReplicas: int(readEnvironmentVariable('CONTAINER_APP_MIN_REPLICAS', '1'))
  targetPort: int(readEnvironmentVariable('CONTAINER_APP_TARGET_PORT', '8080'))
}

param azureAd = {
  domain: readEnvironmentVariable('AZUREAD__DOMAIN', '')
  tenantId: readEnvironmentVariable('AZUREAD__TENANTID', '')
  clientId: readEnvironmentVariable('AZUREAD__CLIENTID', '')
}
