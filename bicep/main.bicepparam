using './main.bicep'

// Resource Group Configuration
param resourceGroupName = 'rg_03-assignment'
param location = 'eastus'

// Existing Storage Account Configuration
// The storage account will be referenced from its current location
param existingStorageAccountName = 'cscie942026bcalderon'
param existingStorageAccountResourceGroup = 'rg_hw3_notekeeper'

// Existing App Service Configuration
// The app service will be referenced from its current location
param existingAppServiceName = 'hw3-notekeeper'
param existingAppServiceResourceGroup = 'rg_hw3_notekeeper'

// Existing App Service Plan Configuration (To Upgrade to B2)
param existingAppServicePlanName = 'ASP-rgclassdemo01-b91b'
param existingAppServicePlanResourceGroup = 'rg_hw3_notekeeper'


