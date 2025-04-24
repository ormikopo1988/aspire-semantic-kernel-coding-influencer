using ChatApp.Infrastructure.Models;
using Microsoft.SemanticKernel;
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
                AgentType.Researcher => "researcher.yaml",
                AgentType.InternalKnowledge => "internalKnowledge.yaml",
                AgentType.Writer => "writer.yaml",
                AgentType.Editor => "editor.yaml",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(agentType),
                    agentType,
                    null),
            };

            return ReadFileForPromptTemplateConfig(
                $"./Agents/Prompts/{promptFile}");
        }

        private static PromptTemplateConfig 
            ReadFileForPromptTemplateConfig(string fileName)
        {
            var yaml = File.ReadAllText(fileName);

            return KernelFunctionYaml.ToPromptTemplateConfig(yaml);
        }
    }
}
