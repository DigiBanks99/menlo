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

resource cosmosRoleDefReadMeta 'Microsoft.DcoumentDB/databaseAccounts/sqlRoleDefinitions@2024-05-15' = {
  name: guid(subscription().id, resourceGroup().id, 'cosmosRoleDefReadMeta')
  parent: cosmos
  properties: {
    roleName: 'Cosmos DB Account Meta Reader'
    description: 'Read-only access to Cosmos DB account metadata'
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

resource cosmosRoleDefContributeMeta 'Microsoft.DcoumentDB/databaseAccounts/sqlRoleDefinitions@2024-05-15' = {
  name: guid(subscription().id, resourceGroup().id, 'cosmosRoleDefContributeMeta')
  parent: cosmos
  properties: {
    roleName: 'Cosmos DB Account Meta Contributor'
    description: 'Contribute acess to Cosmos DB account metadata'
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

/*resource roleAssignmentCosmosDataReaderContainerApp 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(identities.containerApp.principalId!, roleIds.cosmosDbAccountReader, resourceGroup().id)
    scope: cosmos
    properties: {
        principalId: identities.containerApp.principalId!
        roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', roleIds.cosmosDbAccountReader)
    }
}

resource roleAssignmentCosmosDataContributorContainerApp 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(identities.containerApp.principalId!, roleIds.cosmosDbAccountContributor, resourceGroup().id)
    scope: cosmos
    properties: {
        principalId: identities.containerApp.principalId!
        roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', roleIds.cosmosDbAccountContributor)
    }
}*/

resource roleAssignmentCosmosMetaReaderContainerApp 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2022-04-01' = {
  name: guid(subscription().id, resourceGroup().id, cosmosRoleDefReadMeta.id,  identities.containerApp.principalId!)
  parent: cosmos
  properties: {
    principalId: identities.containerApp.principalId!
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', cosmosRoleDefReadMeta.id)
    scope: cosmos.id
  }
}

resource roleAssignmentCosmosMetaContributorContainerApp 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2022-04-01' = {
  name: guid(subscription().id, resourceGroup().id, cosmosRoleDefContributeMeta.id,  identities.containerApp.principalId!)
  parent: cosmos
  properties: {
    principalId: identities.containerApp.principalId!
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', cosmosRoleDefContributeMeta.id)
    scope: cosmos.id
  }
}
