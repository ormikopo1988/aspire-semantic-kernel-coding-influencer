using Aspire.Hosting.Azure;
using Azure.Provisioning.Resources;
using Azure.Provisioning.Search;

var builder = DistributedApplication.CreateBuilder(args);

var azureDeployment = Environment
    .GetEnvironmentVariable("AzureDeployment") 
        ?? "chatdeploymentnew";

var embeddingModelDeployment = Environment
    .GetEnvironmentVariable("EmbeddingModelDeployment") 
        ?? "text-embedding-3-large";

var azureEndpoint = Environment
    .GetEnvironmentVariable("AzureEndpoint");

var vectorStoreCollectionName = Environment
    .GetEnvironmentVariable("VectorStoreCollectionName")
        ?? "default";

var exisitingVectorSearch = !string.IsNullOrEmpty(
    builder.Configuration
        .GetSection("ConnectionStrings")["vectorSearch"]);

var vectorSearch = !builder.ExecutionContext.IsPublishMode && exisitingVectorSearch
    ? builder.AddConnectionString("vectorSearch")
    : builder.AddAzureSearch("vectorSearch")
        .ConfigureInfrastructure(infra =>
        {
            var resources = infra.GetProvisionableResources();

            var searchService = resources.OfType<SearchService>().Single();

            searchService.Identity = new ManagedServiceIdentity
            {
                ManagedServiceIdentityType = 
                    ManagedServiceIdentityType.SystemAssigned,
            };

            searchService.SearchSkuName = SearchServiceSkuName.Free;
            searchService.IsLocalAuthDisabled = false;
            searchService.AuthOptions = new SearchAadAuthDataPlaneAuthOptions
            {
                AadAuthFailureMode = SearchAadAuthFailureMode.Http403
            };
        });

var agentModelDeployment = builder
    .AddBicepTemplate("aoai", "./BicepTemplates/openAi.module.bicep")
    .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
    .WithParameter(AzureBicepResource.KnownParameters.PrincipalType);

var backend = builder.AddProject<Projects.ChatApp_WebApi>("backend")
    .WithReference(vectorSearch)
    .WaitFor(vectorSearch)
    .WithEnvironment("AzureDeployment", azureDeployment)
    .WithEnvironment("EmbeddingModelDeployment", embeddingModelDeployment)
    .WithEnvironment("AzureEndpoint", azureEndpoint)
    .WithEnvironment("VectorStoreCollectionName", vectorStoreCollectionName)
    .WithEnvironment("ConnectionStrings__openAi", agentModelDeployment.GetOutput("connectionString"))
    .WithEnvironment(
        "ModelDeployment", 
        agentModelDeployment.GetOutput("modelDeployment"))
    .WithEnvironment(
        "AIProjectConnectionString", 
        agentModelDeployment.GetOutput("aiProjectConnectionString"))
    .WithExternalHttpEndpoints();

var frontend = builder.AddNpmApp("frontend", "../ChatApp.Client")
    .WithReference(backend)
    .WaitFor(backend)
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.AddProject<Projects.ChatApp_KernelMemory>("chatapp-kernelmemory")
    .ExcludeFromManifest();

builder.Build().Run();