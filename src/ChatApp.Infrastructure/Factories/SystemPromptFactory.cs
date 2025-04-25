using ChatApp.Infrastructure.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Prompty;
using System;
using System.IO;

namespace ChatApp.Infrastructure.Factories
{
    public static class SystemPromptFactory
    {
        public static string GetAgentName(AgentType agentType)
        {
            return agentType switch
            {
                AgentType.Researcher => "Researcher",
                AgentType.InternalKnowledge => "InternalKnowledge",
                AgentType.Writer => "Writer",
                AgentType.Editor => "Editor",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(agentType), 
                    agentType, 
                    null),
            };
        }


        public static PromptTemplateConfig GetAgentPromptTemplateConfig(
            AgentType agentType)
        {
            string? promptFile = agentType switch
            {
                AgentType.Researcher => "researcher.prompty",
                AgentType.InternalKnowledge => "internalKnowledge.prompty",
                AgentType.Writer => "writer.prompty",
                AgentType.Editor => "editor.prompty",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(agentType),
                    agentType,
                    null),
            };

            return ReadPromptyFileForTemplateConfig(
                $"./Agents/Prompts/{promptFile}");
        }

        private static PromptTemplateConfig
            ReadPromptyFileForTemplateConfig(string fileName)
        {
            var yaml = File.ReadAllText(fileName);

            return KernelFunctionPrompty.ToPromptTemplateConfig(yaml);
        }
    }
}
