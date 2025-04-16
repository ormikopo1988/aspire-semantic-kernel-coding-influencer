using ChatApp.Contracts.Models;
using System.Text.Json.Serialization;

namespace ChatApp.Contracts.Responses;

public struct AIChatMessageDelta
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("role")]
    public AIChatRole? Role { get; set; }

    [JsonPropertyName("context"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AIChatAgentInfo? Context { get; set; }
}

public struct AIChatAgentInfo(string Name)
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = Name;
}