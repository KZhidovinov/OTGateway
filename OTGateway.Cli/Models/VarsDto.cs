using System.Text.Json.Serialization;

namespace OTGateway.Cli.Models;

/// <summary>
/// Ответ GET /api/vars — текущее состояние шлюза и котла.
/// Поля сделаны nullable, т.к. прошивка может не отдавать часть данных.
/// </summary>
public sealed class VarsDto
{
    [JsonPropertyName("master")] public MasterDto? Master { get; set; }
    [JsonPropertyName("slave")] public SlaveDto? Slave { get; set; }
}

public sealed class MasterDto
{
    [JsonPropertyName("heating")] public HeatingStateDto? Heating { get; set; }
    [JsonPropertyName("dhw")] public DhwStateDto? Dhw { get; set; }
    [JsonPropertyName("network")] public NetworkStateDto? Network { get; set; }
    [JsonPropertyName("mqtt")] public MqttStateDto? Mqtt { get; set; }
    [JsonPropertyName("emergency")] public EmergencyStateDto? Emergency { get; set; }
    [JsonPropertyName("externalPump")] public PumpStateDto? ExternalPump { get; set; }
    [JsonPropertyName("cascadeControl")] public CascadeStateDto? CascadeControl { get; set; }
}

public sealed class HeatingStateDto
{
    [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    [JsonPropertyName("blocking")] public bool? Blocking { get; set; }
    [JsonPropertyName("indoorTempControl")] public bool? IndoorTempControl { get; set; }
    [JsonPropertyName("overheat")] public bool? Overheat { get; set; }
    [JsonPropertyName("freezing")] public bool? Freezing { get; set; }
    [JsonPropertyName("setpointTemp")] public double? SetpointTemp { get; set; }
    [JsonPropertyName("targetTemp")] public double? TargetTemp { get; set; }
    [JsonPropertyName("currentTemp")] public double? CurrentTemp { get; set; }
    [JsonPropertyName("returnTemp")] public double? ReturnTemp { get; set; }
    [JsonPropertyName("indoorTemp")] public double? IndoorTemp { get; set; }
    [JsonPropertyName("outdoorTemp")] public double? OutdoorTemp { get; set; }
    [JsonPropertyName("minTemp")] public double? MinTemp { get; set; }
    [JsonPropertyName("maxTemp")] public double? MaxTemp { get; set; }
}

public sealed class DhwStateDto
{
    [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    [JsonPropertyName("overheat")] public bool? Overheat { get; set; }
    [JsonPropertyName("targetTemp")] public double? TargetTemp { get; set; }
    [JsonPropertyName("currentTemp")] public double? CurrentTemp { get; set; }
    [JsonPropertyName("returnTemp")] public double? ReturnTemp { get; set; }
    [JsonPropertyName("minTemp")] public double? MinTemp { get; set; }
    [JsonPropertyName("maxTemp")] public double? MaxTemp { get; set; }
}

public sealed class NetworkStateDto
{
    [JsonPropertyName("connected")] public bool? Connected { get; set; }
}

public sealed class MqttStateDto
{
    [JsonPropertyName("connected")] public bool? Connected { get; set; }
}

public sealed class EmergencyStateDto
{
    [JsonPropertyName("state")] public bool? State { get; set; }
}

public sealed class PumpStateDto
{
    [JsonPropertyName("state")] public bool? State { get; set; }
}

public sealed class CascadeStateDto
{
    [JsonPropertyName("input")] public bool? Input { get; set; }
    [JsonPropertyName("output")] public bool? Output { get; set; }
}

public sealed class SlaveDto
{
    [JsonPropertyName("connected")] public bool? Connected { get; set; }
    [JsonPropertyName("flame")] public bool? Flame { get; set; }
    [JsonPropertyName("memberId")] public int? MemberId { get; set; }
    [JsonPropertyName("flags")] public int? Flags { get; set; }
    [JsonPropertyName("type")] public int? Type { get; set; }
    [JsonPropertyName("appVersion")] public double? AppVersion { get; set; }
    [JsonPropertyName("protocolVersion")] public double? ProtocolVersion { get; set; }
    [JsonPropertyName("cooling")] public CoolingDto? Cooling { get; set; }
    [JsonPropertyName("fault")] public CodeDto? Fault { get; set; }
    [JsonPropertyName("diag")] public CodeDto? Diag { get; set; }
    [JsonPropertyName("modulation")] public RangeDto? Modulation { get; set; }
    [JsonPropertyName("power")] public RangeDto? Power { get; set; }
    [JsonPropertyName("heating")] public CircuitDto? Heating { get; set; }
    [JsonPropertyName("dhw")] public CircuitDto? Dhw { get; set; }
}

public sealed class CoolingDto
{
    [JsonPropertyName("active")] public bool? Active { get; set; }
    [JsonPropertyName("setpoint")] public double? Setpoint { get; set; }
}

public sealed class CodeDto
{
    [JsonPropertyName("active")] public bool? Active { get; set; }
    [JsonPropertyName("code")] public int? Code { get; set; }
}

public sealed class RangeDto
{
    [JsonPropertyName("min")] public double? Min { get; set; }
    [JsonPropertyName("max")] public double? Max { get; set; }
}

public sealed class CircuitDto
{
    [JsonPropertyName("active")] public bool? Active { get; set; }
    [JsonPropertyName("minTemp")] public double? MinTemp { get; set; }
    [JsonPropertyName("maxTemp")] public double? MaxTemp { get; set; }
}
