import { CosmosDatabase } from './modules/cosmos.bicep'
import { ContainerApp } from './modules/container-app.bicep'

type CosmosAccount = {
  accountName: string
  database: CosmosDatabase
}

@description('Cosmos Parameters')
param cosmosAccount CosmosAccount

@description('Container App Parameters')
param containerApp ContainerApp

param location string = resourceGroup().location

module cosmos 'modules/cosmos.bicep' = {
  name: 'cosmos'
  params: {
    accountName: cosmosAccount.accountName
    location: location
    database: cosmosAccount.database
  }
}

module app 'modules/container-app.bicep' = {
  name: 'containerApp'
  params: {
    envionrmentName: containerApp.environmentName
    containerAppName: containerApp.containerAppName
    location: location
    revisionSuffix: containerApp.revisionSuffix
    image: containerApp.image
    cpuCore: containerApp.cpuCore
    memorySize: containerApp.memorySize
    minReplicas: containerApp.minReplicas
    maxReplicas: containerApp.maxReplicas
    targetPort: containerApp.targetPort
  }
}

module roleAssignment 'modules/role-assignments.bicep' = {
  name: 'roleAssignment'
  params: {
    identities: [
      {
        key: 'cosmos'
        name: cosmos.outputs.accountName
        resourceId: cosmos.outputs.id
        principalId: cosmos.outputs.identityId
      }
      {
        key: 'containerApp'
        name: app.outputs.containerAppName
        resourceId: app.outputs.id
        principalId: app.outputs.identityId
      }
    ]
  }
}

output cosmosEndpoint string = cosmos.outputs.endpoint
output appFqdn string = app.outputs.fqdn