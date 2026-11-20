using System.Text.Json;
using OTGateway.Cli.Commands;

namespace OTGateway.Cli.Tests;

public class SettingsPatchTests
{
    [Theory]
    [InlineData("true", JsonValueKind.True)]
    [InlineData("false", JsonValueKind.False)]
    [InlineData("True", JsonValueKind.True)]
    [InlineData("55", JsonValueKind.Number)]
    [InlineData("-3.25", JsonValueKind.Number)]
    [InlineData("0", JsonValueKind.Number)]
    [InlineData("hello", JsonValueKind.String)]
    [InlineData("55abc", JsonValueKind.String)]
    [InlineData("", JsonValueKind.String)]
    public void ParseValue_CoercesStrings(string raw, JsonValueKind expected) =>
        Assert.Equal(expected, SettingsCommand.ParseValue(raw).GetValueKind());

    [Fact]
    public void ParseValue_PreservesNumericValue()
    {
        Assert.Equal(55, SettingsCommand.ParseValue("55").GetValue<long>());
        Assert.Equal(0.5, SettingsCommand.ParseValue("0.5").GetValue<double>());
    }

    [Fact]
    public void BuildSettingsPatch_ScalarField_IsMinimalWithoutSiblings()
    {
        // Патч должен содержать ТОЛЬКО указанный лист: jsonToSettings проверяет каждый ключ,
        // поэтому частичный вложенный патч мержится безопасно и не перетирает соседей.
        var patch = SettingsCommand.BuildSettingsPatch("heating.target", "55");

        Assert.Equal("""{"heating":{"target":55}}""", patch.GetRawText());
        Assert.Equal("heating", patch.EnumerateObject().Single().Name);
        // Соседей (minTemp/maxTemp) в патче нет.
        Assert.False(patch.GetProperty("heating").TryGetProperty("minTemp", out _));
    }

    [Fact]
    public void BuildSettingsPatch_BoolField()
    {
        var patch = SettingsCommand.BuildSettingsPatch("pid.enabled", "true");

        Assert.Equal("""{"pid":{"enabled":true}}""", patch.GetRawText());
    }

    [Fact]
    public void BuildSettingsPatch_NestedPath_CreatesIntermediateObjects()
    {
        var patch = SettingsCommand.BuildSettingsPatch("pid.deadband.value", "1.5");

        Assert.Equal("""{"pid":{"deadband":{"value":1.5}}}""", patch.GetRawText());
    }

    [Fact]
    public void BuildSettingsPatch_DeepPath_OnlyLeafNested()
    {
        var patch = SettingsCommand.BuildSettingsPatch("cascadeControl.output.gpio", "7");

        var cc = patch.GetProperty("cascadeControl");
        // Ветка input не попадает в патч — трогается только output.gpio.
        Assert.False(cc.TryGetProperty("input", out _));
        Assert.Equal("""{"gpio":7}""", cc.GetProperty("output").GetRawText());
    }

    [Theory]
    [InlineData("target")]
    [InlineData("")]
    [InlineData(".")]
    public void BuildSettingsPatch_InvalidPath_Throws(string path) =>
        Assert.Throws<ArgumentException>(() => SettingsCommand.BuildSettingsPatch(path, "1"));
}
