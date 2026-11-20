using System.Text.Json.Serialization;

namespace OTGateway.Cli.Models;

/// <summary>
/// Описание датчика из GET /api/sensors?detailed=1.
/// Прошивка в detailed-режиме отдаёт только enabled, name, purpose, connected, signalQuality
/// и поля результата (value либо temperature/humidity/battery/rssi для BLE).
/// Поэтому id/type/gpio/address/mac — необязательные (часто отсутствуют → null → «—» в таблице).
/// </summary>
public sealed class SensorDto
{
    /// <summary>Идентификатор датчика — это ключ словаря в ответе (число), отдельного поля «id» нет.</summary>
    [JsonPropertyName("id")] public int? Id { get; set; }
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    /// <summary>Назначение (purpose) — код, определяет единицы измерения значения.</summary>
    [JsonPropertyName("purpose")] public int Purpose { get; set; }
    /// <summary>Тип датчика — в detailed-ответе отсутствует, поэтому может быть null.</summary>
    [JsonPropertyName("type")] public int? Type { get; set; }
    [JsonPropertyName("gpio")] public int? Gpio { get; set; }
    [JsonPropertyName("address")] public string? Address { get; set; }
    [JsonPropertyName("connected")] public bool Connected { get; set; }
    [JsonPropertyName("signalQuality")] public double? SignalQuality { get; set; }
    [JsonPropertyName("value")] public double? Value { get; set; }
    [JsonPropertyName("temperature")] public double? Temperature { get; set; }
    [JsonPropertyName("humidity")] public double? Humidity { get; set; }
    [JsonPropertyName("battery")] public double? Battery { get; set; }
    [JsonPropertyName("rssi")] public double? Rssi { get; set; }
    [JsonPropertyName("mac")] public string? Mac { get; set; }
}
