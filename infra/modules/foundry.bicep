targetScope = 'resourceGroup'

param projectName string
param location string

// Foundry resource is a variant of a CognitiveServices/account resource type
resource aiFoundry 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: projectName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'S0'
  }
  kind: 'AIServices'
  properties: {
    allowProjectManagement: true
    customSubDomainName: '${projectName}-bremmdev'
    disableLocalAuth: false
  }
}

// Developer APIs are exposed via a project
resource aiProject 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' = {
  name: projectName
  parent: aiFoundry
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {}
}

output documentIntelligenceEndpoint string = aiFoundry.properties.endpoint
output aiFoundryPrincipalId string = aiFoundry.identity.principalId
