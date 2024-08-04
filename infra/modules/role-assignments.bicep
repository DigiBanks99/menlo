@export()
type PrincipalIdentity = {
  key: 'containerApp' | 'cosmos'
  resourceId: string
  name: string
  principalId: string?
}

@description('Principal IDs for specific resources')
param identities PrincipalIdentity[]

var roleIds = {
    cosmosDbAccountReader: 'fbdf93bf-df7d-467e-a4d2-9458aa1360c8'
    cosmosDbAccountContributor: '5bd9cd88-fe45-4216-938b-f97437e15450'
}

var containerAppIdentities = [for identity in identities: identity.key == 'containerApp' ? {
    key: identity.key
    resourceId: identity.resourceId
    name: identity.name
    principalId: identity.principalId
  } : null]

var containerAppIdentity = containerAppIdentities[0]

var cosmosIdentities = [for identity in identities: identity.key == 'cosmos' ? {
    key: identity.key
    resourceId: identity.resourceId
    name: identity.name
  } : null]

var cosmosIdentity = cosmosIdentities[0]

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

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = if (cosmosIdentity != null) {
    name: cosmosIdentity!.name
}

resource roleAssignmentCosmosDataReaderContainerApp 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(subscription().subscriptionId, 'roleAssignmentCosmosDataReaderContainerApp', roleIds.cosmosDbAccountReader, containerAppIdentity!.principalId!)
    scope: cosmos
    properties: {
        principalId: containerAppIdentity!.principalId!
        roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts', cosmosIdentity!.name, roleIds.cosmosDbAccountReader)
    }
}

resource roleAssignmentCosmosDataContributorContainerApp 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(subscription().subscriptionId, 'roleAssignmentCosmosDataContributorContainerApp', roleIds.cosmosDbAccountReader, containerAppIdentity!.principalId!)
    scope: cosmos
    properties: {
        principalId: containerAppIdentity!.principalId!
        roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts', cosmosIdentity!.name, roleIds.cosmosDbAccountContributor)
    }
}

resource roleAssignmentCosmosMetaReaderContainerApp 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(subscription().subscriptionId, 'roleAssignmentCosmosMetaReaderContainerApp', cosmosRoleDefReadMeta.id, cosmosIdentity!.name)
    scope: cosmos
    properties: {
        principalId: containerAppIdentity!.name
        roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts', cosmosIdentity!.name, roleIds.cosmosDbAccountReader)
    }
}

resource roleAssignmentCosmosMetaContributorContainerApp 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(subscription().subscriptionId, 'roleAssignmentCosmosMetaContributorContainerApp', cosmosRoleDefContributeMeta.id, cosmosIdentity!.name)
    scope: cosmos
    properties: {
        principalId: containerAppIdentity!.name
        roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts', cosmosIdentity!.name, roleIds.cosmosDbAccountContributor)
    }
}
