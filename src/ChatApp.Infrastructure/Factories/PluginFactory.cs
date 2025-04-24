using ChatApp.Infrastructure.Models;
using ChatApp.Infrastructure.Plugins.GitHubPlugin;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace ChatApp.Infrastructure.Factories
{
    internal static class PluginFactory
    {
        public static Kernel GetAgentKernel(
            Kernel kernel, 
            AgentType agentType, 
            ILoggerFactory loggerFactory)
        {
            var agentKernel = kernel.Clone();
            
            switch (agentType)
            {
                case AgentType.Researcher:
                    var gitHubPlugin = new GitHubPlugin(
                        loggerFactory.CreateLogger<GitHubPlugin>());

                    agentKernel.Plugins.AddFromObject(gitHubPlugin);
                    
                    break;
            }

            return agentKernel;
        }
    }
}
