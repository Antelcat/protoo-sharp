using System.Runtime.Serialization;
using System.Text.Json;

namespace Antelcat.AspNetCore.ProtooSharp;

internal class Message
{
    public static Dictionary<string, object> Parse(string raw)
    {
        JsonElement obj;
        var         message = new Dictionary<string, object>();
        try
        {
            var json = JsonSerializer.Deserialize<JsonDocument>(raw);
            if (json is not null) obj = json.RootElement;
            else throw new SerializationException($"parse() | invalid JSON:{raw}");
        }
        catch
        {
            throw new SerializationException($"parse() | invalid JSON:{raw}");
        }

        if (obj.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
        {
            throw new SerializationException("parse() | not an object");
        }

        if (obj.TryGetProperty("request", out _))
        {
            message["request"] = true;
            if (!obj.TryGetProperty("method", out var method) || method.ValueKind != JsonValueKind.String)
            {
                throw new JsonException("parse() | missing/invalid method field");
            }

            if (!obj.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.Number)
            {
                throw new JsonException("parse() | missing/invalid id field");
            }

            message[nameof(id)]     = id.GetInt32();
            message[nameof(method)] = method.GetString()!;
            message["data"]         = obj.TryGetProperty("data", out var data) ? data : new JsonElement();
        }
        else if (obj.TryGetProperty("response", out _))
        {
            message["response"] = true;
            if (!obj.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.Number)
            {
                throw new JsonException("parse() | missing/invalid id field");
            }

            message[nameof(id)] = id.GetInt32();
            if (obj.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True)
            {
                message[nameof(ok)] = true;
                message["data"]     = obj.TryGetProperty("data", out var data) ? data : new JsonElement();
            }
            else
            {
                message[nameof(ok)]  = false;
                message["errorCode"] = obj.TryGetProperty("errorCode", out var errorCode) ? errorCode.GetInt32() : -1;
                message["errorReason"] = obj.TryGetProperty("errorReason", out var errorReason)
                    ? errorReason.GetString() ?? string.Empty
                    : string.Empty;
            }
        }
        else if (obj.TryGetProperty("notification", out _))
        {
            message["notification"] = true;
            if (!obj.TryGetProperty("method", out var method) || method.ValueKind != JsonValueKind.String)
            {
                throw new JsonException("parse() | missing/invalid method field");
            }

            message[nameof(method)] = method.GetString()!;
            message["data"]         = obj.TryGetProperty("data", out var data) ? data : new JsonElement();
        }
        else
        {
            throw new InvalidDataException("parse() | missing request/response field");
        }

        return message;
    }
}