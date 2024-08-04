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

var roleIds = {
    cosmosDbAccountReader: 'fbdf93bf-df7d-467e-a4d2-9458aa1360c8'
    cosmosDbAccountContributor: '5bd9cd88-fe45-4216-938b-f97437e15450'
}

resource cosmosRoleDefReadMeta 'Microsoft.Authorization/roleDefinitions@2022-04-01' = {
    name: roleIds.cosmosDbAccountReader
    scope: resourceGroup()
    properties: {
        roleName: 'Cosmos DB Account Meta Reader'
        description: 'Read-only access to Cosmos DB account metadata'
        type: 'CustomRole'
        assignableScopes: [
            '/'
        ]
        permissions: [
            {
                actions: [
                    'Microsoft.DocumentDB/databaseAccounts/readMetadata'
                    'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/read'
                    'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/executeQuery'
                    'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/readChangeFeed'
                ]
                notActions: []
                dataActions: []
                notDataActions: []
            }
        ]
    }
}

resource cosmosRoleDefContributeMeta 'Microsoft.Authorization/roleDefinitions@2022-04-01' = {
    name: roleIds.cosmosDbAccountReader
    scope: resourceGroup()
    properties: {
        roleName: 'Cosmos DB Account Meta Contributor'
        description: 'Contribute acess to Cosmos DB account metadata'
        type: 'CustomRole'
        assignableScopes: [
            '/'
        ]
        permissions: [
            {
                actions: [
                    'Microsoft.DocumentDB/databaseAccounts/readMetadata'
                    'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/*'
                    'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/*'
                ]
                notActions: []
                dataActions: []
                notDataActions: []
            }
        ]
    }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
    name: identities.cosmos.name
}

resource roleAssignmentCosmosDataReaderContainerApp 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(subscription().subscriptionId, 'roleAssignmentCosmosDataReaderContainerApp', roleIds.cosmosDbAccountReader, identities.containerApp.principalId!)
    scope: cosmos
    properties: {
        principalId: identities.containerApp.principalId!
        roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts', identities.cosmos.name, roleIds.cosmosDbAccountReader)
    }
}

resource roleAssignmentCosmosDataContributorContainerApp 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(subscription().subscriptionId, 'roleAssignmentCosmosDataContributorContainerApp', roleIds.cosmosDbAccountReader, identities.containerApp.principalId!)
    scope: cosmos
    properties: {
        principalId: identities.containerApp.principalId!
        roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts', identities.cosmos.name, roleIds.cosmosDbAccountContributor)
    }
}

resource roleAssignmentCosmosMetaReaderContainerApp 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(subscription().subscriptionId, 'roleAssignmentCosmosMetaReaderContainerApp', cosmosRoleDefReadMeta.id, identities.cosmos.name)
    scope: cosmos
    properties: {
        principalId: identities.containerApp.name
        roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts', identities.cosmos.name, roleIds.cosmosDbAccountReader)
    }
}

resource roleAssignmentCosmosMetaContributorContainerApp 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(subscription().subscriptionId, 'roleAssignmentCosmosMetaContributorContainerApp', cosmosRoleDefContributeMeta.id, identities.cosmos.name)
    scope: cosmos
    properties: {
        principalId: identities.containerApp.name
        roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts', identities.cosmos.name, roleIds.cosmosDbAccountContributor)
    }
}
