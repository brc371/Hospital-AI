// Module for deploying Storage Account and App Service resources
// This module is called by main.bicep and deployed to rg_03-assignment

// ========== PARAMETERS ==========

@description('The location for all resources')
param location string

@description('The name of the storage account to create')
param storageAccountName string

@description('The container name to be created in blob storage')
param containerName string

@description('The queue name to be created in storage')
param queueName string

@description('The name of the App Service Plan')
param appServicePlanName string

@description('The name of the App Service web app')
param appServiceName string

@description('The SKU for the storage account')
param storageSkuName string

@description('The access tier for the storage account')
param storageAccessTier string

@description('Allow public blob access')
param allowPublicAccess bool

@description('Default network action for storage')
param networkDefaultAction string

// ========== STORAGE ACCOUNT RESOURCES ==========

// Create the storage account
resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: storageSkuName
  }
  kind: 'StorageV2'
  properties: {
    accessTier: storageAccessTier
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: allowPublicAccess
    networkAcls: {
      defaultAction: networkDefaultAction
    }
  }
}

// Create blob service
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2021-08-01' = {
  parent: storageAccount
  name: 'default'
  properties: {}
}

// Create blob container
resource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-08-01' = {
  parent: blobService
  name: containerName
}

// Create queue service
resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2021-08-01' = {
  parent: storageAccount
  name: 'default'
  properties: {}
}

// Create queue
resource queue 'Microsoft.Storage/storageAccounts/queueServices/queues@2021-08-01' = {
  parent: queueService
  name: queueName
}

// ========== APP SERVICE RESOURCES ==========

// Create App Service Plan (Basic B2)
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

// Create App Service
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: appServiceName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
  }
}

// ========== OUTPUTS ==========

output storageAccountId string = storageAccount.id
output storageAccountName string = storageAccount.name
output containerId string = container.id
output queueId string = queue.id
output appServicePlanId string = appServicePlan.id
output appServiceId string = appService.id
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
