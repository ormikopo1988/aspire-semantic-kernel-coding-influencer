using System.Text.Json.Serialization;
using ChatApp.Contracts.Converters;

namespace ChatApp.Contracts.Models;

[JsonConverter(typeof(JsonCamelCaseEnumConverter<AIChatRole>))]
public enum AIChatRole
{
    System,
    Assistant,
    User
}
