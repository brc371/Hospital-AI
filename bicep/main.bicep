// Main Bicep file for Extra Credit Option 3
// Creates new resource group and references existing storage account and app service
// DEPLOYMENT SCOPE: Subscription Level
// This script orchestrates:
//   3.1 - Create resource group rg_03-assignment
//   3.2 - Reference existing Azure Blob Storage and redeploy to rg_03-assignment
//   3.3 - Reference existing Azure App Service and redeploy to rg_03-assignment
//   3.4 - Update existing App Service plan to B2 (if needed)

targetScope = 'subscription'

// ========== PARAMETERS ==========

@description('The name of the resource group to create for this deployment')
param resourceGroupName string = 'rg_03-assignment'

@description('The location for the resource group')
param location string = 'eastus'

@description('The name of the existing storage account to move to new RG')
param existingStorageAccountName string = 'cscie942026bcalderon'

@description('The resource group of the existing storage account')
param existingStorageAccountResourceGroup string = 'rg_hw3_notekeeper'

@description('The name of the existing app service to move to new RG')
param existingAppServiceName string = 'hw3-notekeeper'

@description('The resource group of the existing app service')
param existingAppServiceResourceGroup string = 'rg_hw3_notekeeper'

@description('The name of the existing App Service plan to upgrade to B2 (optional)')
param existingAppServicePlanName string = 'ASP-rgclassdemo01-b91b'

@description('The resource group of the App Service plan')
param existingAppServicePlanResourceGroup string = 'rg_hw3_notekeeper'

// ========== RESOURCES ==========

// 3.1 Create the new resource group
resource rg03assignment 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
}

// 3.2 & 3.3 Reference and redeploy existing storage account and app service to new RG
module moveStorageAndAppService 'modules/move-storage-and-appservice.bicep' = {
  name: 'moveStorageAndAppService'
  scope: rg03assignment
  params: {
    storageAccountName: existingStorageAccountName
    appServiceName: existingAppServiceName
    existingStorageAccountResourceGroup: existingStorageAccountResourceGroup
    existingAppServiceResourceGroup: existingAppServiceResourceGroup
  }
}

// 3.4 Update existing App Service plan to B2
module upgradeExistingAppServicePlan 'modules/upgrade-appservice-plan.bicep' = {
  name: 'upgradeExistingAppServicePlan'
  scope: resourceGroup(existingAppServicePlanResourceGroup)
  params: {
    appServicePlanName: existingAppServicePlanName
  }
}

// ========== OUTPUTS ==========

output resourceGroupName string = rg03assignment.name
output resourceGroupId string = rg03assignment.id
output storageAccountId string = moveStorageAndAppService.outputs.storageAccountId
output storageAccountName string = moveStorageAndAppService.outputs.storageAccountName
output appServiceId string = moveStorageAndAppService.outputs.appServiceId
output appServiceName string = moveStorageAndAppService.outputs.appServiceName
output appServiceUrl string = moveStorageAndAppService.outputs.appServiceUrl
output upgradedAppServicePlanId string = upgradeExistingAppServicePlan.outputs.appServicePlanId
output upgradedAppServicePlanSku string = upgradeExistingAppServicePlan.outputs.skuName
