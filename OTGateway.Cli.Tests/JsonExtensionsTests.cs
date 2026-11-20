using System.Text.Json;
using OTGateway.Cli.Helpers;
using OTGateway.Cli.Tests.TestHelpers;

namespace OTGateway.Cli.Tests;

[Collection("Console")]
public class JsonExtensionsTests
{
    [Fact]
    public void ReadJsonFile_ParsesContent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"otg-test-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, """{"a":1}""");

            var element = JsonExtensions.ReadJsonFile(path);

            Assert.Equal(1, element.GetProperty("a").GetInt32());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadJsonFile_MissingFile_ThrowsFileNotFound() =>
        Assert.Throws<FileNotFoundException>(() => JsonExtensions.ReadJsonFile(Path.Combine(Path.GetTempPath(), "no-such-otg-file.json")));

    [Fact]
    public void ReadJsonFile_InvalidJson_ThrowsInvalidData()
    {
        var path = Path.Combine(Path.GetTempPath(), $"otg-test-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{oops");

            Assert.Throws<InvalidDataException>(() => JsonExtensions.ReadJsonFile(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadJsonFile_EmptyFile_ThrowsInvalidData()
    {
        var path = Path.Combine(Path.GetTempPath(), $"otg-test-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "   ");

            Assert.Throws<InvalidDataException>(() => JsonExtensions.ReadJsonFile(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void PrintJsonTree_WritesAllValueKinds()
    {
        using var doc = JsonDocument.Parse("""{"a":1,"b":"s","c":true,"d":null}""");
        using var scope = ConsoleScope.Capture();

        JsonExtensions.PrintJsonTree(doc.RootElement);

        var output = scope.Output;
        Assert.Contains("\"a\": 1,", output);
        Assert.Contains("\"b\": \"s\",", output);
        Assert.Contains("\"c\": true,", output);
        // Последний элемент — без завершающей запятой.
        Assert.Contains("\"d\": null", output);
        Assert.DoesNotContain("\"d\": null,", output);
    }

    [Fact]
    public void PrintJsonTree_EmptyContainers()
    {
        using var doc = JsonDocument.Parse("""{"obj":{},"arr":[]}""");
        using var scope = ConsoleScope.Capture();

        JsonExtensions.PrintJsonTree(doc.RootElement);

        var output = scope.Output;
        Assert.Contains("{}", output);
        Assert.Contains("[]", output);
    }

    [Fact]
    public void PrintJsonTree_NestedArrayCommas()
    {
        using var doc = JsonDocument.Parse("""{"e":[{"x":1,"y":2}]}""");
        using var scope = ConsoleScope.Capture();

        JsonExtensions.PrintJsonTree(doc.RootElement);

        var output = scope.Output;
        Assert.Contains("\"x\": 1,", output);
        Assert.Contains("\"y\": 2", output);
        Assert.DoesNotContain("\"y\": 2,", output);
    }

    [Fact]
    public void PrintJsonTree_RedactsPasswordLeaves()
    {
        using var doc = JsonDocument.Parse(
            """{"mqtt":{"server":"1.2.3.4","password":"s3cret"},"portal":{"passWord":"topsecret"},"token":"abc"}""");
        using var scope = ConsoleScope.Capture();

        JsonExtensions.PrintJsonTree(doc.RootElement, redactSecrets: true);

        var output = scope.Output;
        // Секреты замаскированы, обычные строки — как есть.
        Assert.DoesNotContain("s3cret", output);
        Assert.DoesNotContain("topsecret", output);
        Assert.Contains("••••", output);
        Assert.Contains("1.2.3.4", output);
        // «token» не содержит «password» → не маскируется.
        Assert.Contains("\"abc\"", output);
    }

    [Fact]
    public void PrintJsonTree_NoRedactionByDefault()
    {
        using var doc = JsonDocument.Parse("""{"mqtt":{"password":"s3cret"}}""");
        using var scope = ConsoleScope.Capture();

        JsonExtensions.PrintJsonTree(doc.RootElement);

        Assert.Contains("s3cret", scope.Output);
    }

    [Fact]
    public void PrintJsonTree_WritesToProvidedWriter()
    {
        // M9: seam вывода — необязательный TextWriter вместо Console.
        using var doc = JsonDocument.Parse("""{"a":1}""");
        var sw = new StringWriter();

        JsonExtensions.PrintJsonTree(doc.RootElement, writer: sw);

        Assert.Contains("\"a\": 1", sw.ToString());
    }
}

[Collection("Console")]
public class FormatterTableTests
{
    [Fact]
    public void Table_AlignsColumnsToWidestCell()
    {
        using var scope = ConsoleScope.Capture();

        Formatter.Table(["Name", "Value"], [["Датчик длинный", "1"], ["Кр", "22"]]);

        var lines = scope.Output.Split(Environment.NewLine).Where(l => l.Length > 0).ToArray();
        // Заголовок, линия разделителя, две строки данных.
        Assert.Equal(4, lines.Length);
        Assert.StartsWith("Name", lines[0]);
        Assert.StartsWith("─", lines[1]);
        Assert.StartsWith("Датчик длинный", lines[2]);
        Assert.StartsWith("Кр", lines[3]);
        // Колонка значений выровнена по всей ширине самой широкой ячейки.
        Assert.Equal(lines[0].IndexOf("Value", StringComparison.Ordinal), lines[2].IndexOf("1", StringComparison.Ordinal));
        Assert.Equal(lines[0].IndexOf("Value", StringComparison.Ordinal), lines[3].IndexOf("22", StringComparison.Ordinal));
    }

    [Fact]
    public void Table_EmptyRows_PrintsPlaceholder()
    {
        using var scope = ConsoleScope.Capture();

        Formatter.Table(["A"], []);

        Assert.Contains("(нет данных)", scope.Output);
    }

    [Fact]
    public void Table_WritesToProvidedWriter()
    {
        // M9: seam вывода — передаём TextWriter напрямую, не подменяя Console.
        var sw = new StringWriter();

        Formatter.Table(["Name", "Value"], [["x", "1"]], sw);

        var text = sw.ToString();
        Assert.Contains("Name", text);
        Assert.Contains("x", text);
        Assert.Contains("1", text);
    }
}
