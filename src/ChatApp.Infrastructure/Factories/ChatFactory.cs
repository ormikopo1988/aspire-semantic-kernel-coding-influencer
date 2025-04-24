using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using System.Collections.Generic;
using ChatApp.Infrastructure.Strategies;

namespace ChatApp.Infrastructure.Factories
{
    public static class ChatFactory
    {
        public static AgentGroupChat CreateAgentGroupChat(
            Kernel kernel,
            List<Agent> agents,
            Agent initialAgent)
        {
            var agentGroupChat = new AgentGroupChat
            {
                LoggerFactory = kernel.LoggerFactory
            };
            
            foreach (var agent in agents)
            {
                agentGroupChat.AddAgent(agent);
            }

            agentGroupChat.ExecutionSettings = new AgentGroupChatSettings
            {
                SelectionStrategy = new SequentialSelectionStrategy
                {
                    InitialAgent = initialAgent
                },
                TerminationStrategy = new NoFeedbackLeftTerminationStrategy()
            };

            return agentGroupChat;
        }
    }
}
