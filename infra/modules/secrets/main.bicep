// ============================================================================
// Menlo Infrastructure - Secrets Module
// ============================================================================
// This module deploys:
// - Azure Key Vault with RBAC authorization
// - User-assigned managed identity for deployment scripts
// - Self-signed certificate for CD authentication
//
// The certificate is created using a deployment script that runs Azure CLI
// commands in a container instance.
// ============================================================================

// ============================================================================
// Parameters
// ============================================================================

@description('The Azure region for resources')
param location string

@description('The name of the Key Vault')
param keyVaultName string

@description('The name of the certificate to create')
param certificateName string

@description('The object ID of the GitHub Actions service principal')
param githubActionsObjectId string

@description('The object ID of the admin user')
param adminObjectId string

@description('Tags to apply to resources')
param tags object

@description('The validity period of the certificate in months')
@minValue(1)
@maxValue(24)
param certificateValidityMonths int = 12

@description('The subject name for the certificate')
param certificateSubject string = 'CN=menlo-cd.menlo.local'

// ============================================================================
// Variables
// ============================================================================

var managedIdentityName = 'id-${keyVaultName}-deploy'
var deploymentScriptName = 'ds-${keyVaultName}-cert'

// Role definition IDs
// See: https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
var keyVaultAdministratorRoleId = '00482a5a-887f-4fb3-b363-3b7fe8e74483'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
var keyVaultCertificateUserRoleId = 'db79e9a7-68ee-4b58-9aeb-b90e7c24fcba'

// ============================================================================
// User-Assigned Managed Identity
// ============================================================================
// This identity is used by the deployment script to create the certificate

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
  tags: tags
}

// ============================================================================
// Key Vault
// ============================================================================

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// ============================================================================
// RBAC Role Assignments
// ============================================================================

// Admin user gets Key Vault Administrator role (for initial setup and management)
resource adminRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, adminObjectId, keyVaultAdministratorRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultAdministratorRoleId)
    principalId: adminObjectId
    principalType: 'User'
  }
}

// Managed identity gets Key Vault Administrator role (for certificate creation)
resource managedIdentityRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, managedIdentity.id, keyVaultAdministratorRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultAdministratorRoleId)
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// GitHub Actions service principal gets Certificate User role (to download certificate)
resource githubActionsSecretsRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, githubActionsObjectId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: githubActionsObjectId
    principalType: 'ServicePrincipal'
  }
}

resource githubActionsCertRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, githubActionsObjectId, keyVaultCertificateUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultCertificateUserRoleId)
    principalId: githubActionsObjectId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// Deployment Script - Create Self-Signed Certificate
// ============================================================================
// Uses Azure CLI to create a self-signed certificate in Key Vault
// The script runs in a container instance with the managed identity

resource createCertificateScript 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: deploymentScriptName
  location: location
  tags: tags
  kind: 'AzureCLI'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    azCliVersion: '2.63.0'
    retentionInterval: 'P1D'
    timeout: 'PT30M'
    cleanupPreference: 'OnSuccess'
    environmentVariables: [
      {
        name: 'KEY_VAULT_NAME'
        value: keyVault.name
      }
      {
        name: 'CERTIFICATE_NAME'
        value: certificateName
      }
      {
        name: 'CERTIFICATE_SUBJECT'
        value: certificateSubject
      }
      {
        name: 'VALIDITY_MONTHS'
        value: string(certificateValidityMonths)
      }
    ]
    scriptContent: loadTextContent('scripts/create-certificate.sh')
  }
  dependsOn: [
    managedIdentityRoleAssignment
  ]
}

// ============================================================================
// Outputs
// ============================================================================

@description('The name of the Key Vault')
output keyVaultName string = keyVault.name

@description('The URI of the Key Vault')
output keyVaultUri string = keyVault.properties.vaultUri

@description('The name of the certificate')
output certificateName string = certificateName

@description('The certificate thumbprint')
output certificateThumbprint string = createCertificateScript.properties.outputs.thumbprint

@description('The resource ID of the managed identity used for deployment')
output managedIdentityId string = managedIdentity.id
