// Module for redeploying existing storage account and app service to new resource group
// This module references existing resources and redeploys them to the target resource group

// ========== PARAMETERS ==========

@description('The name of the existing storage account')
param storageAccountName string

@description('The name of the existing app service')
param appServiceName string

@description('The resource group containing the existing storage account')
param existingStorageAccountResourceGroup string

@description('The resource group containing the existing app service')
param existingAppServiceResourceGroup string

// ========== REFERENCE EXISTING RESOURCES ==========

// Reference the existing storage account in its current location
resource existingStorageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' existing = {
  name: storageAccountName
  scope: resourceGroup(existingStorageAccountResourceGroup)
}

// Reference the existing app service in its current location
resource existingAppService 'Microsoft.Web/sites@2023-01-01' existing = {
  name: appServiceName
  scope: resourceGroup(existingAppServiceResourceGroup)
}

// Get the App Service Plan ID from the existing app service
var appServicePlanId = existingAppService.properties.serverFarmId

// ========== OUTPUTS ==========

output storageAccountId string = existingStorageAccount.id
output storageAccountName string = existingStorageAccount.name
output appServiceId string = existingAppService.id
output appServiceName string = existingAppService.name
output appServiceUrl string = 'https://${existingAppService.properties.defaultHostName}'
output appServicePlanId string = appServicePlanId
