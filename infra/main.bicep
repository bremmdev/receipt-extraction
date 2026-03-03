targetScope = 'subscription'

param projectName string
param location string
param storageAccountName string

resource rg 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: 'rg-${projectName}'
  location: location
}

module foundry 'modules/foundry.bicep' = {
  scope: rg
  params: {
    projectName: projectName
    location: location
  }
}

module storageAccount 'modules/storageAccount.bicep' = {
  scope: rg
  params: {
    location: location
    storageAccountName: storageAccountName
  }
}

module functionApp 'modules/functionApp.bicep' = {
  scope: rg
  params: {
    projectName: projectName
    location: location
    storageAccountName: storageAccountName
  }
}

module roleAssignments 'modules/roleAssignments.bicep' = {
  scope: rg
  params: {
    storageAccountName: storageAccountName
    functionAppPrincipalId: functionApp.outputs.principalId
  }
}
