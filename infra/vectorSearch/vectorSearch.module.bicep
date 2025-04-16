@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource vectorSearch 'Microsoft.Search/searchServices@2023-11-01' = {
  name: take('vectorsearch-${uniqueString(resourceGroup().id)}', 60)
  location: location
  properties: {
    authOptions: {
      aadOrApiKey: {
        aadAuthFailureMode: 'http403'
      }
    }
    hostingMode: 'default'
    disableLocalAuth: false
    partitionCount: 1
    replicaCount: 1
  }
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'free'
  }
  tags: {
    'aspire-resource-name': 'vectorSearch'
  }
}

output connectionString string = 'Endpoint=https://${vectorSearch.name}.search.windows.net'

output name string = vectorSearch.name