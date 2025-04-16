using ChatApp.KernelMemory.Interfaces;
using ChatApp.KernelMemory.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using System.IO;
using System.Threading.Tasks;
using System;

namespace ChatApp.KernelMemory.Services
{
    public class MemoryService : IMemoryService
    {
        private readonly IKernelMemory _kernelMemory;

        public MemoryService(IConfiguration configuration)
        {
            var apiKey = configuration["AzureOpenAI:ApiKey"]!;
            var deploymentChatName = configuration["AzureOpenAI:DeploymentChatName"]!;
            var deploymentEmbeddingName = configuration["AzureOpenAI:DeploymentEmbeddingName"]!;
            var endpoint = configuration["AzureOpenAI:Endpoint"]!;

            var searchApiKey = configuration["AzureSearch:ApiKey"]!;
            var searchEndpoint = configuration["AzureSearch:Endpoint"]!;

            var embeddingConfig = new AzureOpenAIConfig
            {
                APIKey = apiKey,
                Deployment = deploymentEmbeddingName,
                Endpoint = endpoint,
                APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
                Auth = AzureOpenAIConfig.AuthTypes.APIKey
            };

            var chatConfig = new AzureOpenAIConfig
            {
                APIKey = apiKey,
                Deployment = deploymentChatName,
                Endpoint = endpoint,
                APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
                Auth = AzureOpenAIConfig.AuthTypes.APIKey
            };

            var searchConfig = new AzureAISearchConfig
            {
                APIKey = searchApiKey,
                Endpoint = searchEndpoint,
                UseHybridSearch = true,
                Auth = AzureAISearchConfig.AuthTypes.APIKey
            };

            var directory = Path.GetDirectoryName(Environment.ProcessPath);
            var path = Path.Combine(directory!, "Memory");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            _kernelMemory = new KernelMemoryBuilder()
                .WithAzureOpenAITextGeneration(chatConfig)
                .WithAzureOpenAITextEmbeddingGeneration(embeddingConfig)
                // Uncomment the below line to store the vector database in-memory
                //.WithSimpleVectorDb()
                // Uncomment the below line to store the vector database in Azure AI Search
                .WithAzureAISearchMemoryDb(searchConfig)
                .Build<MemoryServerless>(new KernelMemoryBuilderBuildOptions
                {
                    AllowMixingVolatileAndPersistentData = true
                });
        }

        public async Task<bool> StoreText(string text)
        {
            try
            {
                await _kernelMemory.ImportTextAsync(text);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> StoreFile(string path, string filename)
        {
            try
            {
                var id = filename.Replace(" ", "_");

                await _kernelMemory.ImportDocumentAsync(
                    path,
                    documentId: id,
                    tags: new TagCollection { "title", id });

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> StoreWebsite(string url)
        {
            try
            {
                await _kernelMemory.ImportWebPageAsync(url);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<KernelResponse> AskQuestion(string question)
        {
            var answer = await _kernelMemory.AskAsync(question);

            var response = new KernelResponse
            {
                Answer = answer.Result,
                Citations = answer.RelevantSources
            };

            return response;
        }
    }
}
