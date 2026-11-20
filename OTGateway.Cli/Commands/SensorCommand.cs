using System.Text.Json;
using System.Text.Json.Nodes;
using OTGateway.Cli.Helpers;
using OTGateway.Cli.Models;

namespace OTGateway.Cli.Commands;

/// <summary>
/// otg sensor &lt;list|set|enable|disable&gt; ...
/// </summary>
public static class SensorCommand
{
    public const string Help = """
        Использование: otg sensor <команда> [аргументы]
          list [--json]              список датчиков со значениями
          set <id> <file.json>       обновить датчик (POST /api/sensor?id=)
          enable <id>                включить датчик
          disable <id>               выключить датчик
        """;

    public static async Task<int> RunAsync(CommandContext ctx, string[] args)
    {
        var parsed = Args.Parse(args);
        var sub = parsed[0];

        switch (sub)
        {
            case "list":
                return await ListAsync(ctx);
            case "set":
                return await SetAsync(ctx, parsed[1], parsed[2]);
            case "enable":
                return await SetEnabledAsync(ctx, parsed[1], true);
            case "disable":
                return await SetEnabledAsync(ctx, parsed[1], false);
            case null:
                Console.Error.WriteLine(Help);
                return 2;
            default:
                Console.Error.WriteLine($"Неизвестная подкоманда: {sub}");
                Console.Error.WriteLine(Help);
                return 2;
        }
    }

    public static string UnitForPurpose(int purpose) => purpose switch
    {
        >= 0 and <= 5 => "°C",
        7 => "°C",
        254 => "°C",
        6 => "л/мин",
        8 => "%",
        248 => "%",
        253 => "%",
        249 => "кВт",
        250 => "об/мин",
        251 => "ppm",
        252 => "бар",
        _ => "",
    };

    private static async Task<int> ListAsync(CommandContext ctx)
    {
        var json = await ctx.Gateway.GetSensorsAsync(detailed: true, ctx.Token);

        if (ctx.JsonOutput)
        {
            Console.WriteLine(json.GetRawText());
            return 0;
        }

        var sensors = new List<SensorDto>();
        if (json.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in json.EnumerateObject())
            {
                var sensor = prop.Value.Deserialize<SensorDto>(ctx.Json);
                if (sensor is null)
                {
                    continue;
                }

                // Идентификатор датчика — это ключ словаря (число), отдельного поля «id» прошивка не отдаёт.
                if (sensor.Id is null && int.TryParse(prop.Name, out var key))
                {
                    sensor.Id = key;
                }

                sensors.Add(sensor);
            }
        }

        var headers = new[] { "ID", "Имя", "Назначение", "Тип", "Подкл.", "Значение", "Сигнал" };
        var rows = sensors.Select(s =>
        {
            var unit = UnitForPurpose(s.Purpose);
            var raw = s.Value ?? s.Temperature;
            var value = raw is null ? "—" : $"{raw:N1}{(string.IsNullOrEmpty(unit) ? "" : " " + unit)}";
            return new[]
            {
                s.Id?.ToString() ?? "—",
                s.Name ?? "—",
                $"{s.Purpose}",
                s.Type?.ToString() ?? "—",
                Formatter.Bool(s.Connected),
                value,
                s.SignalQuality is null ? "—" : $"{s.SignalQuality:N0}%",
            };
        }).ToList();

        Formatter.Table(headers, rows);
        return 0;
    }

    private static async Task<int> SetAsync(CommandContext ctx, string? id, string? file)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Console.Error.WriteLine("Укажите id датчика: otg sensor set <id> <file.json>");
            return 2;
        }

        var payload = JsonExtensions.ParseJsonFromFileOrStdin(file);
        await ctx.Gateway.PostSensorAsync(id, payload, ctx.Token);
        Formatter.WriteLineColored($"Датчик {id} обновлён.", ConsoleColor.Green);
        return 0;
    }

    private static async Task<int> SetEnabledAsync(CommandContext ctx, string? id, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Console.Error.WriteLine("Укажите id датчика.");
            return 2;
        }

        using var doc = JsonDocument.Parse(new JsonObject { ["enabled"] = enabled }.ToJsonString());
        await ctx.Gateway.PostSensorAsync(id, doc.RootElement.Clone(), ctx.Token);
        Formatter.WriteLineColored($"Датчик {id}: {(enabled ? "включён" : "выключен")}.", ConsoleColor.Green);
        return 0;
    }
}
