import { CosmosDatabase } from './modules/cosmos.bicep'
import { AzureAd, ContainerApp } from './modules/container-app.bicep'

type CosmosAccount = {
  accountName: string
  database: CosmosDatabase
}

@description('Cosmos Parameters')
param cosmosAccount CosmosAccount

@description('Container App Parameters')
param containerApp ContainerApp

@description('Azure AD Parameters')
param azureAd AzureAd

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
    managedEnvironment: containerApp.environment
    containerAppName: containerApp.containerAppName
    location: location
    revisionSuffix: containerApp.revisionSuffix
    image: containerApp.image
    cpuCore: containerApp.cpuCore
    memorySize: containerApp.memorySize
    minReplicas: containerApp.minReplicas
    maxReplicas: containerApp.maxReplicas
    targetPort: containerApp.targetPort
    cosmos: {
      accountEndpoint: cosmos.outputs.endpoint
      databaseName: cosmosAccount.database.name
    }
    azureAd: azureAd
  }
}

module roleAssignment 'modules/role-assignments.bicep' = {
  name: 'roleAssignment'
  params: {
    identities: {
      cosmos: {
        name: cosmos.outputs.accountName
      }
      containerApp: {
        name: app.outputs.containerAppName
        principalId: app.outputs.identityId
      }
    }
  }
}

output cosmosEndpoint string = cosmos.outputs.endpoint
output appFqdn string = app.outputs.fqdn
