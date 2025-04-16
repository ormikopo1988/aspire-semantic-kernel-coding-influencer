using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Agents.AzureAI;
using System.Text;
using Azure.AI.Projects;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using ChatApp.Contracts.Responses;
using ChatApp.Contracts.Models;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatApp.WebApi.Agents;

public class CreativeWriterSession(
    Kernel kernel, 
    AgentsClient agentsClient, 
    AzureAIAgent researcherAgent,
    AzureAIAgent internalKnowledgeAgent, 
    ChatCompletionAgent writerAgent, 
    ChatCompletionAgent editorAgent)
{
    internal async IAsyncEnumerable<AIChatCompletionDelta> 
        ProcessStreamingRequestAsync(CreateWriterMessage createWriterRequest)
    {
        // Create a conversation thread with the researcher agent
        var researcherAgentThreadResponse = await 
            agentsClient.CreateThreadAsync();
        var researcherAgentThread = 
            researcherAgentThreadResponse.Value;

        StringBuilder researchResults = new();

        await foreach (var response in researcherAgent.InvokeAsync(
            thread: new AzureAIAgentThread(
                agentsClient, 
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
                Context = new AIChatAgentInfo(CreativeWriterApp.ResearcherAgentName),
                Content = response.Message.Content,
            });
        }

        // Create a conversation thread with the internal knowledge agent
        var internalKnowledgeAgentThreadResponse = await 
            agentsClient.CreateThreadAsync();
        var internalKnowledgeAgentThread = 
            internalKnowledgeAgentThreadResponse.Value;

        StringBuilder internalKnowledgeResults = new();

        await foreach (var response in internalKnowledgeAgent
            .InvokeAsync(
                thread: new AzureAIAgentThread(
                    agentsClient, 
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
                Context = new AIChatAgentInfo(CreativeWriterApp.InternalKnowledgeAgentName),
                Content = response.Message.Content,
            });
        }

        writerAgent.Arguments!["research_context"] = createWriterRequest.ResearchContext;
        writerAgent.Arguments["research_results"] = researchResults.ToString();
        writerAgent.Arguments["internal_knowledge_context"] = createWriterRequest.InternalKnowledgeContext;
        writerAgent.Arguments["internal_knowledge_results"] = internalKnowledgeResults.ToString();
        writerAgent.Arguments["assignment"] = createWriterRequest.WritingAsk;

        AgentGroupChat chat = new(writerAgent, editorAgent)
        {
            LoggerFactory = kernel.LoggerFactory,
            ExecutionSettings = new AgentGroupChatSettings
            {
                SelectionStrategy = new SequentialSelectionStrategy 
                { 
                    InitialAgent = writerAgent 
                },
                TerminationStrategy = new NoFeedbackLeftTerminationStrategy()
            }
        };

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

    private sealed class NoFeedbackLeftTerminationStrategy : TerminationStrategy
    {
        // Terminate when the final message contains the term "Article accepted"
        protected override Task<bool> ShouldAgentTerminateAsync(
            Microsoft.SemanticKernel.Agents.Agent agent, 
            IReadOnlyList<ChatMessageContent> history, 
            CancellationToken cancellationToken)
        {
            if (agent.Name != CreativeWriterApp.EditorAgentName)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(history[history.Count - 1]
                .Content?
                .Contains(
                    "Article accepted", 
                    StringComparison.OrdinalIgnoreCase) 
                ?? 
                false);
        }
    }

    public async Task CleanupSessionAsync() 
    {
        // Delete all agents from the session,
        // otherwise they will not be deleted on the backend of Azure AI Agents Service.
        await agentsClient.DeleteAgentAsync(researcherAgent.Id);
        await agentsClient.DeleteAgentAsync(internalKnowledgeAgent.Id);
    }
}