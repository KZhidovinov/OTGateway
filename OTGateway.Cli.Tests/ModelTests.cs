using System.Text.Json;
using OTGateway.Cli.Models;

namespace OTGateway.Cli.Tests;

public class ModelTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
    };

    [Fact]
    public void VarsDto_DeserializesRealisticPayload()
    {
        const string json = """
        {
          "master": {
            "heating": { "enabled": true, "targetTemp": 60.5, "currentTemp": "58.25", "setpointTemp": 57.0, "minTemp": 20, "maxTemp": 80 },
            "dhw": { "enabled": true, "targetTemp": 48, "currentTemp": 45.1 },
            "network": { "connected": true },
            "mqtt": { "connected": false },
            "emergency": { "state": false },
            "externalPump": { "state": true },
            "cascadeControl": { "input": false, "output": false }
          },
          "slave": {
            "connected": true, "flame": true, "memberId": 8, "flags": 4, "type": 122,
            "appVersion": 5.14, "protocolVersion": 3.2,
            "cooling": { "active": false, "setpoint": 0 },
            "fault": { "active": true, "code": 156 },
            "diag": { "active": false, "code": 0 },
            "modulation": { "min": 0, "max": 100 },
            "power": { "min": 2.5, "max": 28 },
            "heating": { "active": true, "minTemp": 20, "maxTemp": 80 },
            "dhw": { "active": false, "minTemp": 30, "maxTemp": 60 }
          }
        }
        """;

        var vars = JsonSerializer.Deserialize<VarsDto>(json, Options);

        Assert.NotNull(vars);
        Assert.True(vars.Master!.Heating!.Enabled);
        Assert.Equal(60.5, vars.Master.Heating.TargetTemp);
        // число пришло строкой — NumberHandling должен вытянуть
        Assert.Equal(58.25, vars.Master.Heating.CurrentTemp);
        Assert.False(vars.Master.Mqtt!.Connected);
        Assert.True(vars.Slave!.Flame);
        Assert.Equal(8, vars.Slave.MemberId);
        Assert.True(vars.Slave.Fault!.Active);
        Assert.Equal(156, vars.Slave.Fault.Code);
        Assert.Equal(28, vars.Slave.Power!.Max);
    }

    [Fact]
    public void VarsDto_MissingFields_AreNull()
    {
        var vars = JsonSerializer.Deserialize<VarsDto>("""{"slave":{"connected":true}}""", Options);

        Assert.Null(vars!.Master);
        Assert.True(vars.Slave!.Connected);
        Assert.Null(vars.Slave.Fault);
        Assert.Null(vars.Slave.Flame);
    }

    [Fact]
    public void SettingsDto_DeserializesNestedSections()
    {
        const string json = """
        {
          "system": { "unitSystem": 0, "logLevel": 4, "telnet": { "enabled": true, "port": 23 }, "ntp": { "server": "pool.ntp.org", "timezone": "Europe/Moscow" } },
          "heating": { "minTemp": 20, "maxTemp": 80, "hysteresis": { "enabled": true, "value": 0.4, "action": 1 } },
          "emergency": { "target": 45, "tresholdTime": 300 },
          "pid": { "enabled": true, "p_factor": 12.5, "deadband": { "enabled": false, "thresholdHigh": 0.5 } },
          "opentherm": { "memberId": 8, "options": { "dhwSupport": true, "nativeOTC": false } },
          "mqtt": { "enabled": true, "server": "192.168.1.2", "port": 1883, "interval": 30 }
        }
        """;

        var s = JsonSerializer.Deserialize<SettingsDto>(json, Options);

        Assert.NotNull(s);
        Assert.Equal(0, s.System!.UnitSystem);
        Assert.Equal(23, s.System.Telnet!.Port);
        Assert.Equal("Europe/Moscow", s.System.Ntp!.Timezone);
        Assert.True(s.Heating!.Hysteresis!.Enabled);
        Assert.Equal(0.4, s.Heating.Hysteresis.Value);
        Assert.Equal(300, s.Emergency!.TresholdTime);
        Assert.Equal(12.5, s.Pid!.PFactor);
        Assert.True(s.OpenTherm!.Options!.DhwSupport);
        Assert.Equal(1883, s.Mqtt!.Port);
    }

    [Fact]
    public void SensorDto_DeserializesDictionary()
    {
        // Реальный GET /api/sensors?detailed=1: без id/type/gpio/mac.
        // Обычный датчик → поле value; BLE → temperature/humidity/battery/rssi.
        const string json = """
        {
          "0": { "enabled": true, "name": "Улица", "purpose": 0, "connected": true, "signalQuality": 100, "value": -12.5 },
          "2": { "enabled": true, "name": "ГВС BLE", "purpose": 4, "connected": false, "signalQuality": 80, "temperature": 44.0, "humidity": 55.0, "battery": 87, "rssi": -66 }
        }
        """;

        var sensors = JsonSerializer.Deserialize<Dictionary<string, SensorDto>>(json, Options);

        Assert.NotNull(sensors);
        Assert.Equal(2, sensors.Count);
        Assert.Equal("Улица", sensors["0"].Name);
        Assert.Equal(-12.5, sensors["0"].Value);
        Assert.True(sensors["0"].Connected);
        // Полей id/type/mac в реальном ответе нет → null (id берётся из ключа словаря в команде).
        Assert.Null(sensors["0"].Type);
        Assert.Null(sensors["0"].Id);
        // BLE-датчик: температура в temperature, отдельного value нет.
        Assert.Equal(44.0, sensors["2"].Temperature);
        Assert.Null(sensors["2"].Value);
        Assert.Equal(87, sensors["2"].Battery);
        Assert.False(sensors["2"].Connected);
    }

    [Theory]
    [InlineData(0, "Open")]
    [InlineData(1, "WEP")]
    [InlineData(2, "WPA")]
    [InlineData(3, "WPA2")]
    [InlineData(4, "WPA/WPA2")]
    [InlineData(5, "WPA/WPA2 Enterprise")]
    [InlineData(6, "WPA3")]
    [InlineData(7, "WPA2/WPA3")]
    [InlineData(8, "WAPI")]
    [InlineData(9, "OWE")]
    [InlineData(10, "WPA3 Enterprise")]
    public void NetworkScanDto_AuthName_MapsCodes(int auth, string expected) =>
        Assert.Equal(expected, new NetworkScanDto { Auth = auth }.AuthName);

    [Fact]
    public void NetworkScanDto_AuthName_UnknownCode() =>
        Assert.Equal("unknown (42)", new NetworkScanDto { Auth = 42 }.AuthName);

    [Fact]
    public void UpgradeStatusResult_SuccessOnlyForStatus2()
    {
        Assert.True(new UpgradeStatusResult { Status = 2 }.Success);
        Assert.False(new UpgradeStatusResult { Status = 4 }.Success);
    }

    [Theory]
    [InlineData(2, "OK")]
    [InlineData(3, "Prohibited")]
    [InlineData(4, "Aborted")]
    [InlineData(5, "Error on start")]
    [InlineData(6, "Error on write")]
    [InlineData(7, "Error on finish")]
    [InlineData(99, "unknown (99)")]
    public void UpgradeStatusResult_StatusText(int status, string expected) =>
        Assert.Equal(expected, new UpgradeStatusResult { Status = status }.StatusText);
}
