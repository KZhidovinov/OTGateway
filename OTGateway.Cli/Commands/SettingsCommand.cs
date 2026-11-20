using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using OTGateway.Cli.Helpers;

namespace OTGateway.Cli.Commands;

/// <summary>
/// otg settings &lt;read|write|set|reset-fault|reset-diag&gt; ...
/// </summary>
public static class SettingsCommand
{
    public static async Task<int> RunAsync(CommandContext ctx, string[] args)
    {
        var parsed = Args.Parse(args);
        var sub = parsed[0];

        switch (sub)
        {
            case "read":
                return await ReadAsync(ctx);
            case "write":
                return await WriteAsync(ctx, parsed[1]);
            case "set":
                return await SetAsync(ctx, parsed[1], parsed[2]);
            case "reset-fault":
                await ctx.Gateway.PostVarsAsync(new { actions = new { resetFault = true } }, ctx.Token);
                Formatter.WriteLineColored("Сброс аварии (resetFault) отправлен.", ConsoleColor.Green);
                return 0;
            case "reset-diag":
                await ctx.Gateway.PostVarsAsync(new { actions = new { resetDiagnostic = true } }, ctx.Token);
                Formatter.WriteLineColored("Сброс диагностики (resetDiagnostic) отправлен.", ConsoleColor.Green);
                return 0;
            case null:
                Console.Error.WriteLine(Help);
                return 2;
            default:
                Console.Error.WriteLine($"Неизвестная подкоманда: {sub}");
                Console.Error.WriteLine(Help);
                return 2;
        }
    }

    public const string Help = """
        Использование: otg settings <команда> [аргументы]
          read [--json]              показать текущие настройки деревом
          write <file.json>          отправить JSON-патч настроек
          set <path> <value>         изменить одно поле, напр. heating.target 55
          reset-fault                сбросить аварию котла (через /api/vars)
          reset-diag                 сбросить диагностику котла (через /api/vars)
        """;

    private static async Task<int> ReadAsync(CommandContext ctx)
    {
        var settings = await ctx.Gateway.GetSettingsAsync(ctx.Token);
        if (ctx.JsonOutput)
        {
            Console.WriteLine(settings.GetRawText());
        }
        else
        {
            // В не-JSON выводе маскируем пароли (mqtt/portal и т.п.).
            JsonExtensions.PrintJsonTree(settings, redactSecrets: true);
        }

        return 0;
    }

    private static async Task<int> WriteAsync(CommandContext ctx, string? file)
    {
        var patch = JsonExtensions.ParseJsonFromFileOrStdin(file);
        var result = await ctx.Gateway.PostSettingsAsync(patch, ctx.Token);
        Formatter.WriteLineColored("Настройки сохранены.", ConsoleColor.Green);
        if (result.ValueKind != JsonValueKind.Undefined && result.ValueKind != JsonValueKind.Null)
        {
            Console.WriteLine(result.GetRawText());
        }

        return 0;
    }

    private static async Task<int> SetAsync(CommandContext ctx, string? path, string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(path) || rawValue is null)
        {
            Console.Error.WriteLine("Укажите путь и значение: otg settings set heating.target 55");
            return 2;
        }

        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            Console.Error.WriteLine("Путь должен быть вида <раздел>.<поле>[.<подполе>], напр. pid.enabled.");
            return 2;
        }

        // Формируем МИНИМАЛЬНЫЙ вложенный патч только по указанному пути (см. BuildSettingsPatch).
        var patch = BuildSettingsPatch(path, rawValue);

        if (ctx.JsonOutput)
        {
            Console.WriteLine(patch.GetRawText());
        }

        await ctx.Gateway.PostSettingsAsync(patch, ctx.Token);
        Formatter.WriteLineColored($"{path} = {rawValue} — сохранено.", ConsoleColor.Green);
        return 0;
    }

    /// <summary>
    /// Строит МИНИМАЛЬНЫЙ вложенный partial-патч настроек по точкам: путь «a.b.c» и значение →
    /// <c>{"a":{"b":{"c":&lt;value&gt;}}}</c>. Соседние поля в патч НЕ попадают.
    ///
    /// Это безопасно для ВСЕХ разделов: в прошивке <c>jsonToSettings</c> (см. src/utils.h) каждый
    /// вложенный парсер (deadband, hysteresis, опентерм-опции, cascade и т.д.) проверяет каждый ключ
    /// через <c>isNull()</c>/<c>is&lt;bool&gt;()</c> и НЕ сбрасывает отсутствующие поля к дефолтам.
    /// Единственные безусловные действия — производные клампы вида <c>maxTemp&lt;minTemp</c>, которые
    /// сравнивают уже установленные значения и идемпотентны. Значит частичный патч мержится корректно
    /// и не перетирает конкурентные изменения/секреты (mqtt.password и т.п.) целиком разделом.
    /// </summary>
    public static JsonElement BuildSettingsPatch(string path, string rawValue)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            throw new ArgumentException("Путь должен быть вида <раздел>.<поле>[.<подполе>], напр. pid.enabled.");
        }

        var root = new JsonObject();
        var current = root;
        for (var i = 0; i < segments.Length - 1; i++)
        {
            var next = new JsonObject();
            current[segments[i]] = next;
            current = next;
        }

        current[segments[^1]] = ParseValue(rawValue);

        return JsonDocument.Parse(root.ToJsonString()).RootElement.Clone();
    }

    /// <summary>Приводит строку к JSON-значению: bool, число или строка.</summary>
    public static JsonNode ParseValue(string raw)
    {
        if (bool.TryParse(raw, out var b))
        {
            return JsonValue.Create(b)!;
        }

        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
        {
            return JsonValue.Create(l)!;
        }

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
        {
            return JsonValue.Create(d)!;
        }

        return JsonValue.Create(raw)!;
    }
}
