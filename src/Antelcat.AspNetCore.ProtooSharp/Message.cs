using System.Runtime.Serialization;
using System.Text.Json;
using Antelcat.AspNetCore.ProtooSharp.Extensions;
using Antelcat.AspNetCore.ProtooSharp.Internals;

namespace Antelcat.AspNetCore.ProtooSharp;


public abstract class Message
{
    internal static Message Parse(string raw)
    {
        JsonElement obj;
        try
        {
            var json = raw.Deserialize<JsonDocument>();
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
            return new RequestMessage(raw)
            {
                Request = raw.Deserialize<RequestPayload>()!
            };
        }

        if (obj.TryGetProperty("response", out _))
        {
            return new ResponseMessage(raw)
            {
                Response = raw.Deserialize<ResponsePayload>()!
            };
        }

        if (obj.TryGetProperty("notification", out _))
        {
            return new NotificationMessage(raw)
            {
                Notification = raw.Deserialize<NotificationPayload>()!
            };
        }

        throw new InvalidDataException("parse() | missing request/response field");

    }

    internal static RequestPayload<T> CreateRequest<T>(string method, T? data) =>
        new()
        {
            Id     = Utils.GenerateRandomNumber(),
            Method = method,
            Data   = data
        };

    internal static ResponsePayload<T> CreateSuccessResponse<T>(RequestPayload request, T? data) =>
        new()
        {
            Id   = request.Id,
            Ok   = true,
            Data = data
        };

    internal static ResponsePayload CreateErrorResponse(RequestPayload request, int errorCode, string errorReason) =>
        new()
        {
            Id          = request.Id,
            ErrorCode   = errorCode,
            ErrorReason = errorReason,
        };

    internal static NotificationPayload<T> CreateNotification<T>(string method, T? data) =>
        new()
        {
            Method = method,
            Data   = data
        };
}



public class RequestMessage(string raw) : Message
{
    public required RequestPayload Request { get; init; }

    public RequestPayload<T>? WithData<T>() => raw.Deserialize<RequestPayload<T>>();
}

public class ResponseMessage(string raw) : Message
{
    public required ResponsePayload Response { get; init; }
    
    public ResponsePayload<T>? WithData<T>() => raw.Deserialize<ResponsePayload<T>>();
}

public class NotificationMessage(string raw) : Message
{
    public required NotificationPayload Notification { get; init; }
    
    public NotificationPayload<T>? WithData<T>() => raw.Deserialize<NotificationPayload<T>>();
}