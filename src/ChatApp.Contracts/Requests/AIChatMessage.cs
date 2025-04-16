using ChatApp.Contracts.Models;
using System;
using System.Text.Json.Serialization;

namespace ChatApp.Contracts.Requests;

public struct AIChatMessage
{
    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("role")]
    public AIChatRole Role { get; set; }

    [JsonPropertyName("context")]
    public BinaryData? Context { get; set; }
}

