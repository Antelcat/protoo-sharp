using System.Text.Json;

namespace Antelcat.AspNetCore.ProtooSharp;

public abstract class Serialization
{
    private class ProtooSerialization : Serialization
    {
        private static JsonSerializerOptions JsonSerializerOptions { get; set; } = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy        = JsonNamingPolicy.CamelCase
        };

        public override string Serialize<T>(T instance) =>
            JsonSerializer.Serialize(instance, JsonSerializerOptions);

        public override T? Deserialize<T>(string json) where T : default => 
            JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);
    }

    public static Serialization GlobalSerialization { get; set; } = new ProtooSerialization();

    public abstract string Serialize<T>(T instance);

    public abstract T? Deserialize<T>(string json);

}