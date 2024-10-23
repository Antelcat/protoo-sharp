
namespace Antelcat.AspNetCore.ProtooSharp.Extensions;

internal static class SerializeExtensions
{
    public static string Serialize<T>(this T instance) => Serialization.GlobalSerialization.Serialize(instance);

    public static T? Deserialize<T>(this string json) => Serialization.GlobalSerialization.Deserialize<T>(json);
}