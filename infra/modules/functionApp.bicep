targetScope = 'resourceGroup'

param projectName string
param location string
param storageAccountName string

resource hostingPlan 'Microsoft.Web/serverfarms@2025-03-01' = {
  name: '${projectName}-asp'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  kind: 'functionapp'
  properties: {}
}

resource functionApp 'Microsoft.Web/sites@2025-03-01' = {
  name: '${projectName}-func'
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      netFrameworkVersion: 'v10.0'
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          // Managed identity auth — uses account name instead of a connection string
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccountName
        }
      ]
    }
  }
}

output principalId string = functionApp.identity.principalId
