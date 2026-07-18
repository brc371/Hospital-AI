// Module for upgrading an existing App Service plan to B2
// This module is deployed to the existing App Service plan's resource group

// ========== PARAMETERS ==========

@description('The name of the existing App Service plan to upgrade')
param appServicePlanName string

@description('The location of the App Service plan')
param location string = resourceGroup().location

// ========== RESOURCES ==========

// Reference and upgrade the existing App Service plan from B1 to B2
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B2'
    tier: 'Basic'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// ========== OUTPUTS ==========

output appServicePlanId string = appServicePlan.id
output appServicePlanName string = appServicePlan.name
output skuName string = appServicePlan.sku.name
output skuTier string = appServicePlan.sku.tier
