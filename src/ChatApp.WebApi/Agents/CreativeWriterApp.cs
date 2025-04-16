using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace ChatApp.WebApi.Agents;

public class CreativeWriterApp
{
    public const string ResearcherAgentName = "Researcher";
    public const string InternalKnowledgeAgentName = "InternalKnowledge";
    public const string WriterAgentName = "Writer";
    public const string EditorAgentName = "Editor";

    private readonly AIProjectClient _aIProjectClient;
    private readonly AgentsClient _agentsClient;
    private readonly Kernel _defaultKernel;
    private readonly IConfiguration _configuration;

    public CreativeWriterApp(Kernel defaultKernel, IConfiguration configuration)
    {
        _defaultKernel = defaultKernel;
        _configuration = configuration;
        _aIProjectClient = new AIProjectClient(
            configuration.GetValue<string>("AIProjectConnectionString")!, 
            new DefaultAzureCredential(
                new DefaultAzureCredentialOptions 
                { 
                    ExcludeVisualStudioCredential = true 
                }), 
            new AIProjectClientOptions());
        _agentsClient = _aIProjectClient.GetAgentsClient();
    }

    internal async Task<CreativeWriterSession> CreateSessionAsync()
    {
        var researcherTemplate = ReadFileForPromptTemplateConfig(
            "./Agents/Prompts/researcher.yaml");
        
        // For the ease of the demo, we are creating an Agent in Azure AI Agent Service 
        // for every session and deleting it after the session finished.
        // For production, you may want to create an agent once and reuse them.
        var researcherAgentDefinition = await _agentsClient
            .CreateAgentAsync(
                model: _configuration.GetValue<string>("ModelDeployment")!,
                name: researcherTemplate.Name,
                description: researcherTemplate.Description,
                instructions: researcherTemplate.Template
            );

        AzureAIAgent researcherAgent = new(
            researcherAgentDefinition,
            _agentsClient,
            templateFactory: new KernelPromptTemplateFactory(),
            templateFormat: PromptTemplateConfig.SemanticKernelTemplateFormat)  
        {
            Name = ResearcherAgentName,
            Kernel = _defaultKernel,
            Arguments = CreateFunctionChoiceAutoBehavior(),
            LoggerFactory = _defaultKernel.LoggerFactory,
        };

        var internalKnowledgeTemplate = ReadFileForPromptTemplateConfig(
            "./Agents/Prompts/internalKnowledge.yaml");

        var connectionsClient = _aIProjectClient.GetConnectionsClient();
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

        // For the ease of the demo, we are creating an Agent in Azure AI Agent Service 
        // for every session and deleting it after the session finished.
        // For production, you may want to create an agent once and reuse them.
        var internalKnowledgeAgentDefinition = await _agentsClient
            .CreateAgentAsync(
                model: _configuration.GetValue<string>("ModelDeployment")!,
                name: internalKnowledgeTemplate.Name,
                description: internalKnowledgeTemplate.Description,
                instructions: internalKnowledgeTemplate.Template,
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

        AzureAIAgent internalKnowledgeAgent = new(
            internalKnowledgeAgentDefinition,
            _agentsClient,
            templateFactory: new KernelPromptTemplateFactory(),
            templateFormat: PromptTemplateConfig.SemanticKernelTemplateFormat)
        {
            Name = InternalKnowledgeAgentName,
            Kernel = _defaultKernel,
            Arguments = CreateFunctionChoiceAutoBehavior(),
            LoggerFactory = _defaultKernel.LoggerFactory,
        };

        ChatCompletionAgent writerAgent = new(
            ReadFileForPromptTemplateConfig(
                "./Agents/Prompts/writer.yaml"), 
            templateFactory: new KernelPromptTemplateFactory())
        {
            Name = WriterAgentName,
            Kernel = _defaultKernel,
            LoggerFactory = _defaultKernel.LoggerFactory
        };

        ChatCompletionAgent editorAgent = new(
            ReadFileForPromptTemplateConfig(
                "./Agents/Prompts/editor.yaml"), 
            templateFactory: new KernelPromptTemplateFactory())
        {
            Name = EditorAgentName,
            Kernel = _defaultKernel,
            LoggerFactory = _defaultKernel.LoggerFactory
        };

        return new CreativeWriterSession(
            _defaultKernel, 
            _agentsClient, 
            researcherAgent, 
            internalKnowledgeAgent, 
            writerAgent, 
            editorAgent);
    }

    private static PromptTemplateConfig ReadFileForPromptTemplateConfig(string fileName)
    {
        var yaml = File.ReadAllText(fileName);

        return KernelFunctionYaml.ToPromptTemplateConfig(yaml);
    }

    private static KernelArguments CreateFunctionChoiceAutoBehavior()
    {
        return new KernelArguments(
            new AzureOpenAIPromptExecutionSettings 
            { 
                FunctionChoiceBehavior = FunctionChoiceBehavior.Required() 
            });
    }
}