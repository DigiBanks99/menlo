@export()
type ContainerApp = {
  environmentName: string
  containerAppName: string
  targetPort: int
  revisionSuffix: string
  image: string
  @description('Number of CPU cores the container can use. Can be with a maximum of two decimals.')
  cpuCore: '0.25' | '0.5' | '0.75' | '1' | '1.25' | '1.5' | '1.75' | '2'
  memorySize: '0.5' | '1' | '1.5' | '2' | '3' | '3.5' | '4'
  @minValue(0)
  @maxValue(25)
  minReplicas: int
  @minValue(0)
  @maxValue(25)
  maxReplicas: int
}

@description('Location for the container app.')
param location string = resourceGroup().location

@description('The name of the container app environment.')
param envionrmentName string

@description('The name of the container app.')
param containerAppName string

@description('The container app target port.')
param targetPort int

@description('The suffix for the container app revision.')
param revisionSuffix string

@description('The container app image.')
param image string

@description('Number of CPU cores the container can use. Can be with a maximum of two decimals.')
@allowed([
  '0.25'
  '0.5'
  '0.75'
  '1'
  '1.25'
  '1.5'
  '1.75'
  '2'
])
param cpuCore string = '0.5'

@description('Amount of memory (in gibibytes, GiB) allocated to the container up to 4GiB. Can be with a maximum of two decimals. Ratio with CPU cores must be equal to 2.')
@allowed([
  '0.5'
  '1'
  '1.5'
  '2'
  '3'
  '3.5'
  '4'
])
param memorySize string = '1'

@description('Minimum number of replicas that will be deployed')
@minValue(0)
@maxValue(25)
param minReplicas int = 1

@description('Maximum number of replicas that will be deployed')
@minValue(0)
@maxValue(25)
param maxReplicas int = 1

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
    name: envionrmentName
    location: location
    properties: {
    }
}

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
    name: containerAppName
    location: location
    identity: {
        type: 'SystemAssigned'
    }
    properties: {
        managedEnvironmentId: containerAppEnv.id
        configuration:{
            ingress:{
                external: true
                targetPort: targetPort
                allowInsecure: false
                traffic: [
                    {
                        latestRevision: true
                        weight: 100
                    }
                ]
            }
        }
        template: {
            revisionSuffix: revisionSuffix
            containers: [
                {
                    name: containerAppName
                    image: image
                    resources: {
                        cpu: json(cpuCore)
                        memory: '${memorySize}Gi'
                    }
                }
            ]
            scale: {
                minReplicas: minReplicas
                maxReplicas: maxReplicas
            }
        }
    }
}

output id string = containerApp.id
output containerAppName string = containerApp.name
output fqdn string = containerApp.properties.configuration.ingress.fqdn
output identityId string = containerApp.identity.principalId
