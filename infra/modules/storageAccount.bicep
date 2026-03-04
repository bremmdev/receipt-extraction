targetScope = 'resourceGroup'

param location string
param storageAccountName string
param deploymentContainerName string = 'func-deployments'

resource storageAccount 'Microsoft.Storage/storageAccounts@2025-06-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2025-06-01' = {
  parent: storageAccount
  name: 'default'
}

// Flex Consumption requires a dedicated blob container where the platform uploads your deployment ZIP files
resource deploymentContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2025-06-01' = {
  parent: blobService
  name: deploymentContainerName
}

output storageAccountName string = storageAccount.name
// Combine here so callers don't need to know the URL structure
output deploymentContainerUrl string = '${storageAccount.properties.primaryEndpoints.blob}${deploymentContainer.name}'

