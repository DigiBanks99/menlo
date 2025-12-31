// ============================================================================
// Menlo Infrastructure - Parameters
// ============================================================================
// This file contains the parameters for the Menlo infrastructure deployment.
//
// Before deploying, update the following parameters:
// - githubActionsObjectId: Get from Azure Portal > Entra ID > App registrations > <your app> > Object ID
// - adminObjectId: Your user object ID from Entra ID
//
// To get your user object ID:
//   az ad signed-in-user show --query id -o tsv
//
// To get the GitHub Actions app object ID:
//   az ad sp show --id <client-id> --query id -o tsv
// ============================================================================

using 'main.bicep'

// The Azure region for all resources
param location = 'southafricarorth'

// The environment name
param environment = 'prod'

// The base name for resources
param baseName = 'menlo'

// The object ID of the GitHub Actions service principal
// This is the service principal that authenticates via OIDC
// Get this from: az ad sp show --id <AZURE_CLIENT_ID> --query id -o tsv
param githubActionsObjectId = '<REPLACE_WITH_GITHUB_ACTIONS_SP_OBJECT_ID>'

// The object ID of the admin user (for initial Key Vault access)
// Get this from: az ad signed-in-user show --query id -o tsv
param adminObjectId = '<REPLACE_WITH_ADMIN_USER_OBJECT_ID>'

// Tags for resource management
param tags = {
  project: 'menlo'
  environment: 'prod'
  managedBy: 'bicep'
  repository: 'menlo'
}
