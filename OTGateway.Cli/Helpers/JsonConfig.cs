using System.Text.Json;
using System.Text.Json.Serialization;

namespace OTGateway.Cli.Helpers;

/// <summary>
/// Единая точка настройки <see cref="JsonSerializerOptions"/> для всего клиента.
/// </summary>
internal static class JsonConfig
{
    /// <summary>Опции чтения (десериализация DTO из ответов устройства).</summary>
    public static readonly JsonSerializerOptions Reading = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    /// <summary>Опции записи (сериализация payload'ов, отправляемых на устройство).</summary>
    public static readonly JsonSerializerOptions Writing = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
