using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using System.Text;
using Azure.AI.Projects;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatApp.Contracts.Responses;
using ChatApp.Contracts.Models;
using Microsoft.SemanticKernel.ChatCompletion;
using ChatApp.Infrastructure.Factories;
using ChatApp.Infrastructure.Models;
using System.Linq;

namespace ChatApp.WebApi.Agents;

public class CreativeWriterSession
{
    private readonly Kernel _kernel;
    private readonly AgentsClient _agentsClient;
    private readonly AzureAIAgent _researcherAgent;
    private readonly AzureAIAgent _internalKnowledgeAgent;
    private readonly ChatCompletionAgent _writerAgent;
    private readonly ChatCompletionAgent _editorAgent;

    public CreativeWriterSession(
        Kernel kernel,
        AgentsClient agentsClient,
        AzureAIAgent researcherAgent,
        AzureAIAgent internalKnowledgeAgent,
        ChatCompletionAgent writerAgent,
        ChatCompletionAgent editorAgent)
    {
        _kernel = kernel;
        _agentsClient = agentsClient;
        _researcherAgent = researcherAgent;
        _internalKnowledgeAgent = internalKnowledgeAgent;
        _writerAgent = writerAgent;
        _editorAgent = editorAgent;
    }

    internal async IAsyncEnumerable<AIChatCompletionDelta> 
        ProcessStreamingRequestAsync(CreateWriterMessage createWriterRequest)
    
    {
        // Create a conversation thread with the researcher agent
        var researcherAgentThreadResponse = await 
            _agentsClient.CreateThreadAsync();

        var researcherAgentThread = 
            researcherAgentThreadResponse.Value;

        StringBuilder researchResults = new();

        await foreach (var response in _researcherAgent.InvokeAsync(
            thread: new AzureAIAgentThread(
                _agentsClient, 
                researcherAgentThread.Id),
            options: new AgentInvokeOptions
            {
                KernelArguments = new KernelArguments
                {
                    { 
                        "research_context", 
                        createWriterRequest.ResearchContext 
                    }
                }
            }))
        {
            researchResults.AppendLine(response.Message.Content);

            yield return new AIChatCompletionDelta(Delta: new AIChatMessageDelta
            {
                Role = AIChatRole.Assistant,
                Context = new AIChatAgentInfo(
                    SystemPromptFactory.GetAgentName(
                        AgentType.Researcher)),
                Content = response.Message.Content,
            });
        }

        // Create a conversation thread with the internal knowledge agent
        var internalKnowledgeAgentThreadResponse = await 
            _agentsClient.CreateThreadAsync();
        var internalKnowledgeAgentThread = 
            internalKnowledgeAgentThreadResponse.Value;

        StringBuilder internalKnowledgeResults = new();

        await foreach (var response in _internalKnowledgeAgent
            .InvokeAsync(
                thread: new AzureAIAgentThread(
                    _agentsClient, 
                    internalKnowledgeAgentThread.Id),
                messages: 
                [
                    new ChatMessageContent(
                        AuthorRole.Assistant,
                        researchResults.ToString())
                ],
                options: new AgentInvokeOptions
                {
                    KernelArguments = new KernelArguments
                    {
                        { 
                            "internal_knowledge_context", 
                            createWriterRequest.InternalKnowledgeContext 
                        }
                    }
                }
            ))
        {
            internalKnowledgeResults.AppendLine(response.Message.Content);

            yield return new AIChatCompletionDelta(Delta: new AIChatMessageDelta
            {
                Role = AIChatRole.Assistant,
                Context = new AIChatAgentInfo(
                    SystemPromptFactory.GetAgentName(
                        AgentType.InternalKnowledge)),
                Content = response.Message.Content,
            });
        }

        _writerAgent.Arguments!["research_context"] = createWriterRequest.ResearchContext;
        _writerAgent.Arguments["research_results"] = researchResults.ToString();
        _writerAgent.Arguments["internal_knowledge_context"] = createWriterRequest.InternalKnowledgeContext;
        _writerAgent.Arguments["internal_knowledge_results"] = internalKnowledgeResults.ToString();
        _writerAgent.Arguments["assignment"] = createWriterRequest.WritingAsk;

        var chat = ChatFactory
            .CreateAgentGroupChat(
                _kernel,
                [ _writerAgent, _editorAgent ],
                _writerAgent);

        await foreach (var response in chat.InvokeAsync())
        {
            yield return new AIChatCompletionDelta(Delta: new AIChatMessageDelta
            {
                Role = AIChatRole.Assistant,
                Context = new AIChatAgentInfo(response.AuthorName ?? ""),
                Content = response.Content,
            });
        }
    }

    public async Task CleanupSessionAsync() 
    {
        // Delete all agent threads from the session,
        // otherwise they will not be deleted on the backend of Azure AI Agents Service.
        var agentThreads = await _agentsClient.GetThreadsAsync();

        if (agentThreads?.Value is not null
            && agentThreads.Value.Any())
        {
            foreach (var thread in agentThreads.Value)
            {
                await _agentsClient
                    .DeleteThreadAsync(thread.Id);
            }
        }

        // Delete all agents from the session,
        // otherwise they will not be deleted on the backend of Azure AI Agents Service.
        await _agentsClient.DeleteAgentAsync(_researcherAgent.Id);
        await _agentsClient.DeleteAgentAsync(_internalKnowledgeAgent.Id);
    }
}