targetScope = 'resourceGroup'

param projectName string
param location string
param storageAccountName string
param deploymentContainerUrl string

resource hostingPlan 'Microsoft.Web/serverfarms@2025-03-01' = {
  name: '${projectName}-asp'
  location: location
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  kind: 'functionapp'
  properties: {
    reserved: true // Flex Consumption is Linux-only
  }
}

resource functionApp 'Microsoft.Web/sites@2025-03-01' = {
  name: '${projectName}-func'
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: deploymentContainerUrl
          authentication: {
            type: 'SystemAssignedIdentity'
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 10
        instanceMemoryMB: 2048 // Valid options: 512, 2048, 4096
        // Uncomment to pre-warm instances and reduce cold starts:
        // alwaysReady: [{ name: 'http', instanceCount: 1 }]
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
    }
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccountName
        }
      ]
    }
  }
}
output principalId string = functionApp.identity.principalId
