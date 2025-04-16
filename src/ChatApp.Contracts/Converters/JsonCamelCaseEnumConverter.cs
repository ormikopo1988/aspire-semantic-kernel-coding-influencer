using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatApp.Contracts.Converters;

public class JsonCamelCaseEnumConverter<T> : JsonStringEnumConverter<T> where T : struct, Enum
{
    public JsonCamelCaseEnumConverter() : base(JsonNamingPolicy.CamelCase)
    {
    }
}
