using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.SemanticKernel;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ChatApp.Infrastructure.Factories;
using ChatApp.Infrastructure.Models;

namespace ChatApp.WebApi.Agents;

public class CreativeWriterApp
{
    private readonly AIProjectClient _aiProjectClient;
    private readonly AgentsClient _agentsClient;
    private readonly Kernel _defaultKernel;
    private readonly IConfiguration _configuration;

    public CreativeWriterApp(Kernel defaultKernel, IConfiguration configuration)
    {
        _defaultKernel = defaultKernel;
        _configuration = configuration;
        _aiProjectClient = new AIProjectClient(
            configuration.GetValue<string>("AIProjectConnectionString")!, 
            new DefaultAzureCredential(
                new DefaultAzureCredentialOptions 
                { 
                    ExcludeVisualStudioCredential = true 
                }), 
            new AIProjectClientOptions());
        _agentsClient = _aiProjectClient.GetAgentsClient();
    }

    internal async Task<CreativeWriterSession> CreateSessionAsync()
    {
        var modelDeployment = _configuration.GetValue<string>("ModelDeployment")!;

        // For the ease of the demo, we are creating an Agent in Azure AI Agent Service 
        // for every session and deleting it after the session finishes.
        // For production, you may want to create an agent once and reuse them.
        var researcherAgent = await CustomAgentFactory
            .CreateAzureAIAgentAsync(
                _agentsClient, 
                _defaultKernel, 
                AgentType.Researcher,
                modelDeployment);

        var connectionsClient = _aiProjectClient.GetConnectionsClient();
        ListConnectionsResponse searchConnections = await connectionsClient
            .GetConnectionsAsync(ConnectionType.AzureAISearch);
        var searchConnection = searchConnections.Value[0];

        var aiSearchIndexResource = new AISearchIndexResource(
            searchConnection.Id,
            _configuration["VectorStoreCollectionName"]!
        )
        {
            QueryType = AzureAISearchQueryType.Simple
        };

        var internalKnowledgeAgent = await CustomAgentFactory
            .CreateAzureAIAgentAsync(
                _agentsClient,
                _defaultKernel,
                AgentType.InternalKnowledge,
                modelDeployment,
                tools: [new AzureAISearchToolDefinition()],
                toolResources: new()
                {
                    AzureAISearch = new()
                    {
                        IndexList =
                        {
                            aiSearchIndexResource
                        }
                    }
                });

        var writerAgent = CustomAgentFactory
            .CreateChatCompletionAgent(
                _defaultKernel,
                AgentType.Writer);

        var editorAgent = CustomAgentFactory
            .CreateChatCompletionAgent(
                _defaultKernel,
                AgentType.Editor);

        return new CreativeWriterSession(
            _defaultKernel, 
            _agentsClient, 
            researcherAgent, 
            internalKnowledgeAgent, 
            writerAgent, 
            editorAgent);
    }
}