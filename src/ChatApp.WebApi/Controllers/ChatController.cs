using ChatApp.Contracts.Models;
using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;
using ChatApp.WebApi.Agents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ChatApp.WebApi.Controllers;

[ApiController, Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly CreativeWriterApp _creativeWriterApp;
    private readonly IDeserializer _yamlDeserializer;

    public ChatController(CreativeWriterApp creativeWriterApp)
    {
        _creativeWriterApp = creativeWriterApp;
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    [HttpPost("stream")]
    [Consumes("application/json")]
    public async Task ProcessStreamingMessage(AIChatRequest request)
    {
        var response = Response;

        response.Headers.Append("Content-Type", "application/x-ndjson");

        var session = await _creativeWriterApp.CreateSessionAsync();

        try
        {
            var userInput = request.Messages.Last();
            var createWriterRequest = _yamlDeserializer
                .Deserialize<CreateWriterMessage>(userInput.Content);

            await foreach (var delta in session
                .ProcessStreamingRequestAsync(createWriterRequest))
            {
                await response.WriteAsync($"{JsonSerializer.Serialize(delta)}\r\n");

                await response.Body.FlushAsync();
            }
        }
        catch (YamlException ex)
        {
            var delta = new AIChatCompletionDelta(Delta: new AIChatMessageDelta
            {
                Role = AIChatRole.System,
                Content = "Error: Invalid YAML format, Details:  \n" + ex,
            });

            await response.WriteAsync($"{JsonSerializer.Serialize(delta)}\r\n");

            await response.Body.FlushAsync();
        }
        finally
        {
            // Cleanup the session (i.e., delete the Agent from AI Agent Service backend)
            await session.CleanupSessionAsync();
        }
    }
}