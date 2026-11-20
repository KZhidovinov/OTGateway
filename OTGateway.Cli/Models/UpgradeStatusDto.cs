using System.Text.Json.Serialization;

namespace OTGateway.Cli.Models;

/// <summary>
/// Ответ POST /api/upgrade.
/// </summary>
public sealed class UpgradeStatusDto
{
    [JsonPropertyName("firmware")] public UpgradeStatusResult? Firmware { get; set; }
    [JsonPropertyName("filesystem")] public UpgradeStatusResult? Filesystem { get; set; }
}

/// <summary>
/// Статус обновления одного компонента.
/// Status: 2=Success, 3=Prohibited, 4=Aborted, 5=ErrorOnStart, 6=ErrorOnWrite, 7=ErrorOnFinish.
/// </summary>
public sealed class UpgradeStatusResult
{
    [JsonPropertyName("status")] public int Status { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }

    public bool Success => Status == 2;

    public string StatusText => Status switch
    {
        2 => "OK",
        3 => "Prohibited",
        4 => "Aborted",
        5 => "Error on start",
        6 => "Error on write",
        7 => "Error on finish",
        _ => $"unknown ({Status})",
    };
}
