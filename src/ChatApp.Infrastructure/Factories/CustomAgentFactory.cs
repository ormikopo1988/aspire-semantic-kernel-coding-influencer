using Azure.AI.Projects;
using ChatApp.Infrastructure.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatApp.Infrastructure.Factories
{
    public static class CustomAgentFactory
    {
        public static ChatCompletionAgent CreateChatCompletionAgent(
            Kernel kernel,
            AgentType agentType)
        {
            var agentPromptTemplateConfig = SystemPromptFactory
                .GetAgentPromptTemplateConfig(agentType);

            return new ChatCompletionAgent(
                agentPromptTemplateConfig,
                new KernelPromptTemplateFactory())
            {
                Name = SystemPromptFactory.GetAgentName(agentType),
                Description = agentPromptTemplateConfig.Description,
                Instructions = $"""{agentPromptTemplateConfig.Template}""",
                Kernel = PluginFactory.GetAgentKernel(
                    kernel,
                    agentType,
                    kernel.LoggerFactory),
                Arguments = CreateFunctionChoiceAutoBehavior(),
                LoggerFactory = kernel.LoggerFactory
            };
        }

        public static async Task<AzureAIAgent> CreateAzureAIAgentAsync(
            AgentsClient agentsClient,
            Kernel kernel,
            AgentType agentType,
            string modelDeployment,
            IEnumerable<ToolDefinition>? tools = null, 
            ToolResources? toolResources = null)
        {
            var agentPromptTemplateConfig = SystemPromptFactory
                .GetAgentPromptTemplateConfig(agentType);

            var agentDefinition = await agentsClient
                .CreateAgentAsync(
                    model: modelDeployment,
                    name: agentPromptTemplateConfig.Name,
                    description: agentPromptTemplateConfig.Description,
                    instructions: agentPromptTemplateConfig.Template,
                    tools: tools,
                    toolResources: toolResources
                );

            return new AzureAIAgent(
                agentDefinition,
                agentsClient,
                templateFactory: new KernelPromptTemplateFactory(),
                templateFormat: PromptTemplateConfig.SemanticKernelTemplateFormat)
                {
                    Name = SystemPromptFactory.GetAgentName(agentType),
                    Description = agentPromptTemplateConfig.Description,
                    Instructions = $"""{agentPromptTemplateConfig.Template}""",
                    Kernel = PluginFactory.GetAgentKernel(
                        kernel,
                        agentType,
                        kernel.LoggerFactory),
                    Arguments = CreateFunctionChoiceAutoBehavior(),
                    LoggerFactory = kernel.LoggerFactory
                };
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
}
