@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

param principalType string

param aiProjectName string = take('aiProject-${uniqueString(resourceGroup().id)}', 64)

param vectorSearchName string = take('vectorsearch-${uniqueString(resourceGroup().id)}', 60)

param applicationInsightsName string = take('chatappapplicationinsights-${uniqueString(resourceGroup().id)}', 260)

var keyVaultName = take('keyVault-${uniqueString(resourceGroup().id)}', 64)

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: keyVaultName
  location: location
  tags: {
    'aspire-resource-name': 'keyVault'
  }
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    accessPolicies: !empty(principalId) ? [
      {
        objectId: principalId
        permissions: { secrets: [ 'get', 'list' ] }
        tenantId: subscription().tenantId
      }
    ] : []
    enabledForDeployment: false
    enabledForTemplateDeployment: false
  }
}

var containers = [
  {
    name: 'default'
  }
]

var files = [
  {
    name: 'default'
  }
]

var queues = [
  {
    name: 'default'
  }
]

var tables = [
  {
    name: 'default'
  }
]

var corsRules = [
  {
    allowedOrigins: [
      'https://mlworkspace.azure.ai'
      'https://ml.azure.com'
      'https://*.ml.azure.com'
      'https://ai.azure.com'
      'https://*.ai.azure.com'
      'https://mlworkspacecanary.azure.ai'
      'https://mlworkspace.azureml-test.net'
    ]
    allowedMethods: [
      'GET'
      'HEAD'
      'POST'
      'PUT'
      'DELETE'
      'OPTIONS'
      'PATCH'
    ]
    maxAgeInSeconds: 1800
    exposedHeaders: [
      '*'
    ]
    allowedHeaders: [
      '*'
    ]
  }
]

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: take('sa${uniqueString(resourceGroup().id)}', 8)
  location: location
  tags: {
    'aspire-resource-name': 'storageAccount'
  }
  kind: 'StorageV2'
  sku: { 
    name: 'Standard_LRS' 
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: true
    allowCrossTenantReplication: true
    allowSharedKeyAccess: true
    defaultToOAuthAuthentication: false
    dnsEndpointType: 'Standard'
    isHnsEnabled: false
    minimumTlsVersion: 'TLS1_2'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
    publicNetworkAccess: 'Enabled'
    supportsHttpsTrafficOnly: true
  }

  resource blobServices 'blobServices' = if (!empty(containers)) {
    name: 'default'
    properties: {
      cors: {
        corsRules: corsRules
      }
      deleteRetentionPolicy: {
        allowPermanentDelete: false
        enabled: false
      }
    }
    resource container 'containers' = [for container in containers: {
      name: container.name
      properties: {
        publicAccess: 'None'
      }
    }]
  }

  resource fileServices 'fileServices' = if (!empty(files)) {
    name: 'default'
    properties: {
      cors: {
        corsRules: corsRules
      }
      shareDeleteRetentionPolicy: {
        enabled: true
        days: 7
      }
    }
  }

  resource queueServices 'queueServices' = if (!empty(queues)) {
    name: 'default'
    properties: {

    }
    resource queue 'queues' = [for queue in queues: {
      name: queue.name
      properties: {
        metadata: {}
      }
    }]
  }

  resource tableServices 'tableServices' = if (!empty(tables)) {
    name: 'default'
    properties: {}
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: take('la-${uniqueString(resourceGroup().id)}', 8)
  location: location
  tags: {
    'aspire-resource-name': 'logAnalytics'
  }
  properties: any({
    retentionInDays: 30
    features: {
      searchVersion: 1
    }
    sku: {
      name: 'PerGB2018'
    }
  })
}

resource openAi 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: take('openAi-${uniqueString(resourceGroup().id)}', 64)
  location: location
  tags: {
    'aspire-resource-name': 'openAi'
  }
  kind: 'OpenAI'
  properties: {
    customSubDomainName: toLower(take(concat('openAi', uniqueString(resourceGroup().id)), 24))
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
  sku: {
    name: 'S0'
  }
}

resource openAi_CognitiveServicesOpenAIContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(
    openAi.id,
    principalId,
    subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442')
  )
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      'a001fd3d-188f-4b5d-821b-7da978bf7442'
    )
    principalType: principalType
  }
  scope: openAi
}

resource chatdeploymentnew 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openAi
  name: 'chatdeploymentnew'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o-mini'
      version: '2024-07-18'
    }
  }
  sku: {
    name: 'Standard'
    capacity: 150
  }
}

resource text_embedding_3_large 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  name: 'text-embedding-3-large'
  parent: openAi
  dependsOn: [
    chatdeploymentnew
  ]
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-3-large'
      version: '1'
    }
  }
  sku: {
    name: 'Standard'
    capacity: 8
  }
}

resource vectorSearch 'Microsoft.Search/searchServices@2023-11-01' existing = {
  name: vectorSearchName
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}

resource aiHub 'Microsoft.MachineLearningServices/workspaces@2024-10-01' = {
  name: take('aiHub-${uniqueString(resourceGroup().id)}', 64)
  location: location
  tags: {
    'aspire-resource-name': 'aiHub'
  }
  sku: {
    name: 'Basic'
    tier: 'Free'
  }
  kind: 'Hub'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    storageAccount: storageAccount.id
    keyVault: keyVault.id
    applicationInsights: applicationInsights.id
    hbiWorkspace: false
    managedNetwork: {
      isolationMode: 'Disabled'
    }
    v1LegacyMode: false
    publicNetworkAccess: 'Enabled'
  }
  
  resource aiServicesConnection 'connections@2024-01-01-preview' = {
    name: 'AzureOpenAI'
    properties: {
      category: 'AzureOpenAI'
      authType: 'AAD'
      isSharedToAll: true
      target: openAi.properties.endpoint
      metadata: {
        ApiType: 'Azure'
        ResourceId: openAi.id
      }
    }
  }
  
  resource aiSearchConnection 'connections@2024-01-01-preview' = {
    name: 'AzureAISearch'
    properties: {
      category: 'CognitiveSearch'
      authType: 'AAD'
      isSharedToAll: true
      target: 'https://${vectorSearch.name}.search.windows.net/'
      metadata: {
        ApiType: 'Azure'
        ResourceId: vectorSearch.id
      }
    }
  }
}

// For constructing project connection string
var subscriptionId = subscription().subscriptionId
var resourceGroupName = resourceGroup().name
var projectConnectionString = '${location}.api.azureml.ms;${subscriptionId};${resourceGroupName};${aiProjectName}'

resource aiProject 'Microsoft.MachineLearningServices/workspaces@2023-08-01-preview' = {
  name: aiProjectName
  location: location
  tags: {
    ProjectConnectionString: projectConnectionString
    'aspire-resource-name': 'aiProject'
  }
  sku: {
    name: 'Basic'
    tier: 'Free'
  }
  kind: 'project'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    hubResourceId: aiHub.id 
    hbiWorkspace: false
    v1LegacyMode: false
    publicNetworkAccess: 'Enabled'
  }
}

resource keyVaultAccess 'Microsoft.KeyVault/vaults/accessPolicies@2022-07-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [ {
        objectId: aiProject.identity.principalId
        tenantId: subscription().tenantId
        permissions: { 
          secrets: [ 
            'get', 'list' 
          ] 
        }
      } 
    ]
  }
}

resource mlServiceRoleDataScientist 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiProject.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f6c7c914-8db3-469d-8ca1-694a8f32e121'))
  properties: {
    principalId: aiProject.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f6c7c914-8db3-469d-8ca1-694a8f32e121')
    principalType: 'ServicePrincipal'
  }
}

resource mlServiceRoleSecretsReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiProject.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ea01e6af-a1c1-4350-9563-ad00f8c72ec5'))
  properties: {
    principalId: aiProject.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ea01e6af-a1c1-4350-9563-ad00f8c72ec5')
    principalType: 'ServicePrincipal'
  }
}

// Azure AI Developer for App MSI over AI Project
resource aiProject_AzureAIDeveloper 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(
    aiProject.id,
    principalId,
    subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '64702f94-c441-49e6-a78b-ef80e0188fee')
  )
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '64702f94-c441-49e6-a78b-ef80e0188fee'
    )
    principalType: principalType
  }
  scope: aiProject
}

// Azure AI Developer for AI Project over AOAI
resource aoai_AzureAIDeveloper 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(
    openAi.id,
    subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '64702f94-c441-49e6-a78b-ef80e0188fee')
  )
  properties: {
    principalId: aiProject.identity.principalId
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '64702f94-c441-49e6-a78b-ef80e0188fee'
    )
    principalType: 'ServicePrincipal'
  }
  scope: openAi
}

// Search Index Data Reader for AI Project over Azure AI Search
resource aiProject_SearchIndexDataReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(
    vectorSearch.id,
    subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '1407120a-92aa-4202-b7e9-c0e197c71c8f')
  )
  properties: {
    principalId: aiProject.identity.principalId
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '1407120a-92aa-4202-b7e9-c0e197c71c8f'
    )
    principalType: 'ServicePrincipal'
  }
  scope: vectorSearch
}

// Search Service Contributor for AI Project over Azure AI Search
resource aiProject_SearchServiceContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(
    vectorSearch.id,
    subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0')
  )
  properties: {
    principalId: aiProject.identity.principalId
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '7ca78c08-252a-4471-8644-bb5ff32d4ba0'
    )
    principalType: 'ServicePrincipal'
  }
  scope: vectorSearch
}

output modelDeployment string = chatdeploymentnew.name
output connectionString string = openAi.properties.endpoint
output aiProjectConnectionString string = projectConnectionString
