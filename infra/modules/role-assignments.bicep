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
    principalId: identity.principalId
  } : null]

var cosmosIdentity = cosmosIdentities[0]

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = if (cosmosIdentity != null) {
    name: cosmosIdentity!.name
}

resource containerAppCosmosRoleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(subscription().subscriptionId, 'containerAppCosmosRoleAssignment', containerAppIdentity!.principalId!)
    scope: cosmos
    properties: {
        principalId: containerAppIdentity!.principalId!
        roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts', cosmosIdentity!.name, roleIds.cosmosDbAccountReader)
    }
}
