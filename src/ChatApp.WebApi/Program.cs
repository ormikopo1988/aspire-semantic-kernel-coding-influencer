using Azure.Identity;
using ChatApp.Contracts.Models;
using ChatApp.Infrastructure.Plugins.GitHubPlugin;
using ChatApp.WebApi.Agents;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureOpenAIClient("openAi", configureSettings: settings =>
{
    settings.Credential = new DefaultAzureCredential(
        new DefaultAzureCredentialOptions 
        { 
            ExcludeVisualStudioCredential = true 
        });
});

builder.AddAzureSearchClient("vectorSearch", configureSettings: settings =>
{
    settings.Credential = new DefaultAzureCredential(
        new DefaultAzureCredentialOptions
        {
            ExcludeVisualStudioCredential = true
        });
});

builder.Services
    .AddKernel()
    .AddAzureOpenAIChatCompletion(builder.Configuration["AzureDeployment"]!)
    .AddAzureAISearchVectorStore()
    .AddAzureOpenAITextEmbeddingGeneration(builder.Configuration["EmbeddingModelDeployment"]!)
    .ConfigureOpenTelemetry(builder.Configuration);

builder.Services.AddSingleton(sp => 
    KernelPluginFactory.CreateFromType<GitHubPlugin>(serviceProvider: sp));

builder.Services.AddTransient<CreativeWriterApp>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions
        .Converters
        .Add(new JsonStringEnumConverter<AIChatRole>(
            JsonNamingPolicy.CamelCase)));

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();