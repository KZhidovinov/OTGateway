using System.Text.Json.Serialization;

namespace OTGateway.Cli.Models;

/// <summary>
/// Результат точки доступа из GET /api/network/scan.
/// </summary>
public sealed class NetworkScanDto
{
    [JsonPropertyName("ssid")] public string? Ssid { get; set; }
    [JsonPropertyName("bssid")] public string? Bssid { get; set; }
    [JsonPropertyName("hidden")] public bool Hidden { get; set; }
    [JsonPropertyName("signalQuality")] public int SignalQuality { get; set; }
    [JsonPropertyName("auth")] public int Auth { get; set; }

    /// <summary>Человекочитаемое имя типа авторизации по коду API.</summary>
    public string AuthName => Auth switch
    {
        0 => "Open",
        1 => "WEP",
        2 => "WPA",
        3 => "WPA2",
        4 => "WPA/WPA2",
        5 => "WPA/WPA2 Enterprise",
        6 => "WPA3",
        7 => "WPA2/WPA3",
        8 => "WAPI",
        9 => "OWE",
        10 => "WPA3 Enterprise",
        _ => $"unknown ({Auth})",
    };
}
