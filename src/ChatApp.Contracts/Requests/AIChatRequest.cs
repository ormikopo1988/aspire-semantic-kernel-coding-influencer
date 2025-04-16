using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatApp.Contracts.Requests;

public record AIChatRequest([property: JsonPropertyName("messages")] IList<AIChatMessage> Messages)
{
    [JsonInclude, JsonPropertyName("sessionState")]
    public string? SessionState;

    [JsonInclude, JsonPropertyName("context")]
    public BinaryData? Context;
}
