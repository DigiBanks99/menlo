// ============================================================================
// Menlo Infrastructure - Main Deployment
// ============================================================================
// This Bicep file orchestrates the deployment of all Menlo infrastructure.
// Currently deploys:
// - Azure Key Vault with self-signed certificate for CD authentication
//
// Usage:
//   az deployment sub create \
//     --location <location> \
//     --template-file main.bicep \
//     --parameters main.bicepparam
// ============================================================================

targetScope = 'subscription'

// ============================================================================
// Parameters
// ============================================================================

@description('The Azure region for all resources')
param location string

@description('The environment name (e.g., prod, dev)')
@allowed(['prod', 'dev'])
param environment string = 'prod'

@description('The base name for resources')
param baseName string = 'menlo'

@description('The object ID of the GitHub Actions service principal for OIDC authentication')
param githubActionsObjectId string

@description('The object ID of the user who will administer Key Vault (for initial setup)')
param adminObjectId string

@description('Tags to apply to all resources')
param tags object = {
  project: 'menlo'
  environment: environment
  managedBy: 'bicep'
}

// ============================================================================
// Variables
// ============================================================================

var resourceGroupName = 'rg-${baseName}-${environment}'
var keyVaultName = 'kv-${baseName}-${environment}'
var certificateName = '${baseName}-cd-cert'

// ============================================================================
// Resource Group
// ============================================================================

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// ============================================================================
// Secrets Module (Key Vault + Certificate)
// ============================================================================

module secrets 'modules/secrets/main.bicep' = {
  name: 'secrets-deployment'
  scope: resourceGroup
  params: {
    location: location
    keyVaultName: keyVaultName
    certificateName: certificateName
    githubActionsObjectId: githubActionsObjectId
    adminObjectId: adminObjectId
    tags: tags
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The name of the resource group')
output resourceGroupName string = resourceGroup.name

@description('The name of the Key Vault')
output keyVaultName string = secrets.outputs.keyVaultName

@description('The URI of the Key Vault')
output keyVaultUri string = secrets.outputs.keyVaultUri

@description('The name of the certificate')
output certificateName string = secrets.outputs.certificateName

@description('The certificate thumbprint (available after certificate creation)')
output certificateThumbprint string = secrets.outputs.certificateThumbprint
