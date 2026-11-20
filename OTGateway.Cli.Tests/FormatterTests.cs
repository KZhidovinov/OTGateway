using System.Globalization;
using OTGateway.Cli.Helpers;

namespace OTGateway.Cli.Tests;

public class FormatterTests : IDisposable
{
    private readonly CultureInfo _original = CultureInfo.CurrentCulture;

    public FormatterTests() => CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

    public void Dispose() => CultureInfo.CurrentCulture = _original;

    [Theory]
    [InlineData(true, "✔")]
    [InlineData(false, "✘")]
    public void Bool_Formats(bool value, string expected) =>
        Assert.Equal(expected, Formatter.Bool(value));

    [Fact]
    public void Bool_Null_IsDash() =>
        Assert.Equal("—", Formatter.Bool(null));

    [Theory]
    [InlineData(45.67, "45.7 °C")]
    [InlineData(0, "0.0 °C")]
    [InlineData(100.0, "100.0 °C")]
    public void Temp_OneDecimal(double value, string expected) =>
        Assert.Equal(expected, Formatter.Temp(value));

    [Fact]
    public void Temp_Null_IsDash() =>
        Assert.Equal("—", Formatter.Temp(null));

    [Fact]
    public void Temp_CustomUnit() =>
        Assert.Equal("21.0 °F", Formatter.Temp(21.0, "°F"));

    [Theory]
    [InlineData(12.34, "12.3")]
    [InlineData(7, "7.0")]
    public void Num_OneDecimal(double value, string expected) =>
        Assert.Equal(expected, Formatter.Num(value));

    [Fact]
    public void Num_Null_IsDash() =>
        Assert.Equal("—", Formatter.Num(null));
}
