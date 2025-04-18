@export()
type PrincipalIdentity = {
  name: string
  principalId: string?
}

@export()
type IdentityMap = {
  cosmos: PrincipalIdentity
  containerApp: PrincipalIdentity
}

@description('Principal IDs for specific resources')
param identities IdentityMap

/*var roleIds = {
    cosmosDbAccountReader: 'fbdf93bf-df7d-467e-a4d2-9458aa1360c8'
    cosmosDbAccountContributor: '5bd9cd88-fe45-4216-938b-f97437e15450'
}*/

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: identities.cosmos.name
}

resource cosmosRoleDefReadMeta 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2021-04-15' = {
  parent: cosmos
  name: guid(subscription().id, resourceGroup().id, 'cosmosRoleDefReadMeta')
  properties: {
    roleName: 'Cosmos DB Account Meta Reader'
    type: 'CustomRole'
    assignableScopes: [
      cosmos.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/read'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/executeQuery'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/readChangeFeed'
        ]
      }
    ]
  }
}

resource cosmosRoleDefContributeMeta 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2021-04-15' = {
  parent: cosmos
  name: guid(subscription().id, resourceGroup().id, 'cosmosRoleDefContributeMeta')
  properties: {
    roleName: 'Cosmos DB Account Meta Contributor'
    type: 'CustomRole'
    assignableScopes: [
      cosmos.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/*'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/*'
        ]
      }
    ]
  }
}

resource roleAssignmentCosmosMetaReaderContainerApp 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15' = {
  name: guid(subscription().id, resourceGroup().id, cosmosRoleDefReadMeta.id,  identities.containerApp.principalId!)
  parent: cosmos
  properties: {
    principalId: identities.containerApp.principalId!
    roleDefinitionId: cosmosRoleDefReadMeta.id
    scope: cosmos.id
  }
}

resource roleAssignmentCosmosMetaContributorContainerApp 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15' = {
  name: guid(subscription().id, resourceGroup().id, cosmosRoleDefContributeMeta.id,  identities.containerApp.principalId!)
  parent: cosmos
  properties: {
    principalId: identities.containerApp.principalId!
    roleDefinitionId: cosmosRoleDefContributeMeta.id
    scope: cosmos.id
  }
}
