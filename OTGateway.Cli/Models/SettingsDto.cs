using System.Text.Json.Serialization;

namespace OTGateway.Cli.Models;

/// <summary>
/// Ответ GET /api/settings — полные настройки шлюза.
/// Соответствует структуре настроек прошивки OTGateway.
/// </summary>
public sealed class SettingsDto
{
    [JsonPropertyName("system")] public SystemSettings? System { get; set; }
    [JsonPropertyName("portal")] public PortalSettings? Portal { get; set; }
    [JsonPropertyName("heating")] public HeatingSettings? Heating { get; set; }
    [JsonPropertyName("dhw")] public DhwSettings? Dhw { get; set; }
    [JsonPropertyName("emergency")] public EmergencySettings? Emergency { get; set; }
    [JsonPropertyName("equitherm")] public EquithermSettings? Equitherm { get; set; }
    [JsonPropertyName("pid")] public PidSettings? Pid { get; set; }
    [JsonPropertyName("opentherm")] public OpenThermSettings? OpenTherm { get; set; }
    [JsonPropertyName("mqtt")] public MqttSettings? Mqtt { get; set; }
    [JsonPropertyName("externalPump")] public ExternalPumpSettings? ExternalPump { get; set; }
    [JsonPropertyName("cascadeControl")] public CascadeControlSettings? CascadeControl { get; set; }
}

public sealed class SystemSettings
{
    /// <summary>0 = метрическая, 1 = имперская.</summary>
    [JsonPropertyName("unitSystem")] public int? UnitSystem { get; set; }
    [JsonPropertyName("statusLedGpio")] public int? StatusLedGpio { get; set; }
    [JsonPropertyName("serial")] public SerialSettings? Serial { get; set; }
    [JsonPropertyName("telnet")] public TelnetSettings? Telnet { get; set; }
    [JsonPropertyName("ntp")] public NtpSettings? Ntp { get; set; }
    [JsonPropertyName("logLevel")] public int? LogLevel { get; set; }
}

public sealed class SerialSettings
{
    [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    [JsonPropertyName("baudrate")] public int? Baudrate { get; set; }
}

public sealed class TelnetSettings
{
    [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    [JsonPropertyName("port")] public int? Port { get; set; }
}

public sealed class NtpSettings
{
    [JsonPropertyName("server")] public string? Server { get; set; }
    [JsonPropertyName("timezone")] public string? Timezone { get; set; }
}

public sealed class PortalSettings
{
    [JsonPropertyName("login")] public string? Login { get; set; }
    [JsonPropertyName("password")] public string? Password { get; set; }
    [JsonPropertyName("auth")] public bool? Auth { get; set; }
    [JsonPropertyName("mdns")] public bool? Mdns { get; set; }
}

public sealed class HeatingSettings
{
    [JsonPropertyName("minTemp")] public double? MinTemp { get; set; }
    [JsonPropertyName("maxTemp")] public double? MaxTemp { get; set; }
    [JsonPropertyName("turboFactor")] public double? TurboFactor { get; set; }
    [JsonPropertyName("maxModulation")] public double? MaxModulation { get; set; }
    [JsonPropertyName("hysteresis")] public HysteresisSettings? Hysteresis { get; set; }
    [JsonPropertyName("overheatProtection")] public ProtectionSettings? OverheatProtection { get; set; }
    [JsonPropertyName("freezeProtection")] public ProtectionSettings? FreezeProtection { get; set; }
    [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    [JsonPropertyName("turbo")] public bool? Turbo { get; set; }
    [JsonPropertyName("target")] public double? Target { get; set; }
}

public sealed class HysteresisSettings
{
    [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    [JsonPropertyName("value")] public double? Value { get; set; }
    [JsonPropertyName("action")] public int? Action { get; set; }
}

public sealed class ProtectionSettings
{
    [JsonPropertyName("highTemp")] public double? HighTemp { get; set; }
    [JsonPropertyName("lowTemp")] public double? LowTemp { get; set; }
}

public sealed class DhwSettings
{
    [JsonPropertyName("minTemp")] public double? MinTemp { get; set; }
    [JsonPropertyName("maxTemp")] public double? MaxTemp { get; set; }
    [JsonPropertyName("maxModulation")] public double? MaxModulation { get; set; }
    [JsonPropertyName("overheatProtection")] public ProtectionSettings? OverheatProtection { get; set; }
    [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    [JsonPropertyName("target")] public double? Target { get; set; }
}

public sealed class EmergencySettings
{
    [JsonPropertyName("target")] public double? Target { get; set; }
    /// <summary>Опечатка "treshold" — имя поля API, намеренно сохранено.</summary>
    [JsonPropertyName("tresholdTime")] public int? TresholdTime { get; set; }
}

public sealed class EquithermSettings
{
    [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    [JsonPropertyName("slope")] public double? Slope { get; set; }
    [JsonPropertyName("exponent")] public double? Exponent { get; set; }
    [JsonPropertyName("shift")] public double? Shift { get; set; }
    [JsonPropertyName("targetDiffFactor")] public double? TargetDiffFactor { get; set; }
}

public sealed class PidSettings
{
    [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    [JsonPropertyName("p_factor")] public double? PFactor { get; set; }
    [JsonPropertyName("i_factor")] public double? IFactor { get; set; }
    [JsonPropertyName("d_factor")] public double? DFactor { get; set; }
    [JsonPropertyName("dt")] public double? Dt { get; set; }
    [JsonPropertyName("minTemp")] public double? MinTemp { get; set; }
    [JsonPropertyName("maxTemp")] public double? MaxTemp { get; set; }
    [JsonPropertyName("deadband")] public DeadbandSettings? Deadband { get; set; }
}

public sealed class DeadbandSettings
{
    [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    [JsonPropertyName("p_multiplier")] public double? PMultiplier { get; set; }
    [JsonPropertyName("i_multiplier")] public double? IMultiplier { get; set; }
    [JsonPropertyName("d_multiplier")] public double? DMultiplier { get; set; }
    [JsonPropertyName("thresholdHigh")] public double? ThresholdHigh { get; set; }
    [JsonPropertyName("thresholdLow")] public double? ThresholdLow { get; set; }
}

public sealed class OpenThermSettings
{
    [JsonPropertyName("unitSystem")] public int? UnitSystem { get; set; }
    [JsonPropertyName("inGpio")] public int? InGpio { get; set; }
    [JsonPropertyName("outGpio")] public int? OutGpio { get; set; }
    [JsonPropertyName("rxLedGpio")] public int? RxLedGpio { get; set; }
    [JsonPropertyName("memberId")] public int? MemberId { get; set; }
    [JsonPropertyName("flags")] public int? Flags { get; set; }
    [JsonPropertyName("minPower")] public double? MinPower { get; set; }
    [JsonPropertyName("maxPower")] public double? MaxPower { get; set; }
    [JsonPropertyName("options")] public OpenThermOptions? Options { get; set; }
}

public sealed class OpenThermOptions
{
    [JsonPropertyName("dhwSupport")] public bool? DhwSupport { get; set; }
    [JsonPropertyName("coolingSupport")] public bool? CoolingSupport { get; set; }
    [JsonPropertyName("summerWinterMode")] public bool? SummerWinterMode { get; set; }
    [JsonPropertyName("heatingStateToSummerWinterMode")] public bool? HeatingStateToSummerWinterMode { get; set; }
    [JsonPropertyName("ch2AlwaysEnabled")] public bool? Ch2AlwaysEnabled { get; set; }
    [JsonPropertyName("heatingToCh2")] public bool? HeatingToCh2 { get; set; }
    [JsonPropertyName("dhwToCh2")] public bool? DhwToCh2 { get; set; }
    [JsonPropertyName("dhwBlocking")] public bool? DhwBlocking { get; set; }
    [JsonPropertyName("dhwStateAsDhwBlocking")] public bool? DhwStateAsDhwBlocking { get; set; }
    [JsonPropertyName("maxTempSyncWithTargetTemp")] public bool? MaxTempSyncWithTargetTemp { get; set; }
    [JsonPropertyName("getMinMaxTemp")] public bool? GetMinMaxTemp { get; set; }
    [JsonPropertyName("ignoreDiagState")] public bool? IgnoreDiagState { get; set; }
    [JsonPropertyName("autoFaultReset")] public bool? AutoFaultReset { get; set; }
    [JsonPropertyName("autoDiagReset")] public bool? AutoDiagReset { get; set; }
    [JsonPropertyName("setDateAndTime")] public bool? SetDateAndTime { get; set; }
    [JsonPropertyName("nativeOTC")] public bool? NativeOTC { get; set; }
    [JsonPropertyName("immergasFix")] public bool? ImmergasFix { get; set; }
    [JsonPropertyName("alwaysSendIndoorTemp")] public bool? AlwaysSendIndoorTemp { get; set; }
}

public sealed class MqttSettings
{
    [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    [JsonPropertyName("homeAssistantDiscovery")] public bool? HomeAssistantDiscovery { get; set; }
    [JsonPropertyName("server")] public string? Server { get; set; }
    [JsonPropertyName("port")] public int? Port { get; set; }
    [JsonPropertyName("user")] public string? User { get; set; }
    [JsonPropertyName("password")] public string? Password { get; set; }
    [JsonPropertyName("prefix")] public string? Prefix { get; set; }
    [JsonPropertyName("interval")] public int? Interval { get; set; }
}

public sealed class ExternalPumpSettings
{
    [JsonPropertyName("use")] public bool? Use { get; set; }
    [JsonPropertyName("gpio")] public int? Gpio { get; set; }
    [JsonPropertyName("invertState")] public bool? InvertState { get; set; }
    [JsonPropertyName("postCirculationTime")] public int? PostCirculationTime { get; set; }
    [JsonPropertyName("antiStuckInterval")] public int? AntiStuckInterval { get; set; }
    [JsonPropertyName("antiStuckTime")] public int? AntiStuckTime { get; set; }
}

public sealed class CascadeControlSettings
{
    [JsonPropertyName("input")] public CascadeInput? Input { get; set; }
    [JsonPropertyName("output")] public CascadeOutput? Output { get; set; }
}

public sealed class CascadeInput
{
    [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    [JsonPropertyName("invertState")] public bool? InvertState { get; set; }
    [JsonPropertyName("gpio")] public int? Gpio { get; set; }
    [JsonPropertyName("thresholdTime")] public int? ThresholdTime { get; set; }
}

public sealed class CascadeOutput
{
    [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
    [JsonPropertyName("invertState")] public bool? InvertState { get; set; }
    [JsonPropertyName("gpio")] public int? Gpio { get; set; }
    [JsonPropertyName("thresholdTime")] public int? ThresholdTime { get; set; }
    [JsonPropertyName("onFault")] public bool? OnFault { get; set; }
    [JsonPropertyName("onLossConnection")] public bool? OnLossConnection { get; set; }
    [JsonPropertyName("onEnabledHeating")] public bool? OnEnabledHeating { get; set; }
}
