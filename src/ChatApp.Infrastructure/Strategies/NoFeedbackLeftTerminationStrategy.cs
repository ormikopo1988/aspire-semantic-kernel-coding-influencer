using ChatApp.Infrastructure.Factories;
using ChatApp.Infrastructure.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Chat;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ChatApp.Infrastructure.Strategies
{
    internal sealed class NoFeedbackLeftTerminationStrategy : TerminationStrategy
    {
        // Terminate when the final message contains the term "Article accepted"
        protected override Task<bool> ShouldAgentTerminateAsync(
            Microsoft.SemanticKernel.Agents.Agent agent,
            IReadOnlyList<ChatMessageContent> history,
            CancellationToken cancellationToken)
        {
            if (agent.Name != SystemPromptFactory
                .GetAgentName(AgentType.Editor))
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
}
