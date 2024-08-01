@export()
type CosmosContainerPartitionKeys = {
    paths: string[]
    kind: 'Hash' | 'Range' | 'MultiHash'
}

@export()
type CosmosContainerPath = {
    path: string
}

@export()
type CosmosContainerIndexingPolicy = {
    indexingMode: 'consistent' | 'lazy' | 'none'
    includedPaths: CosmosContainerPath[]?
    excludedPaths: CosmosContainerPath[]?
}

@export()
type CosmosContainer = {
  name: string
  partitionKeys: CosmosContainerPartitionKeys
  indexingPolicy: CosmosContainerIndexingPolicy
}

@export()
type ComsosDatabaseOptions = {
  throughput: int
}

@export()
type CosmosDatabase = {
  name: string
  options: ComsosDatabaseOptions
  containers: CosmosContainer[]
}

@description('Cosmos DB account name')
param accountName string

@description('Location for the Cosmos DB account.')
param location string = resourceGroup().location

@description('The database parameters')
param database CosmosDatabase

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: toLower(accountName)
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    defaultIdentity: 'SystemAssigned'
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    capabilities: [
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
      maxIntervalInSeconds: 5
      maxStalenessPrefix: 100
    }
    enableAnalyticalStorage: false
    enableAutomaticFailover: false
    enableCassandraConnector: false
    enableMultipleWriteLocations: false
    enableFreeTier: true
  }
}

resource databaseResource 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: account
  name: database.name
  properties: {
    resource: {
      id: database.name
    }
    options: {
      throughput: 1000
    }
  }
}

resource containerResource 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = [
  for container in database.containers: {
    parent: databaseResource
    name: container.name
    properties: {
      resource: {
        id: container.name
        partitionKey: {
          paths: [for partitionKey in container.partitionKeys.paths: partitionKey]
          kind: container.partitionKeys.kind
        }
        indexingPolicy: {
          indexingMode: container.indexingPolicy.indexingMode
          includedPaths: container.indexingPolicy.?includedPaths ?? []
          excludedPaths: container.indexingPolicy.?excludedPaths ?? [
            {
              path: '/_etag/?'
            }
          ]
        }
      }
    }
  }
]

output id string = account.id
output accountName string = account.name
output resourceGroupName string = resourceGroup().name
output resourceId string = account.id
output endpoint string = account.properties.documentEndpoint
output identityId string = account.identity.principalId
