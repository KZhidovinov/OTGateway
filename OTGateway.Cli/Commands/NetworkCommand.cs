using System.Text.Json;
using System.Text.Json.Nodes;
using OTGateway.Cli.Helpers;
using OTGateway.Cli.Models;

namespace OTGateway.Cli.Commands;

/// <summary>
/// otg network &lt;scan|set|info&gt; ...
/// </summary>
public static class NetworkCommand
{
    public const string Help = """
        Использование: otg network <команда> [аргументы]
          scan [--json]                      список точек доступа Wi-Fi
          set [--hostname H] [--use-dhcp B]
              [--static-ip IP] [--gateway GW] [--subnet MASK] [--dns DNS]
                                             обновить сетевые настройки
          info                               состояние сети из /api/vars
        """;

    public static async Task<int> RunAsync(CommandContext ctx, string[] args)
    {
        var parsed = Args.Parse(args);
        var sub = parsed[0];

        switch (sub)
        {
            case "scan":
                return await ScanAsync(ctx);
            case "set":
                return await SetAsync(ctx, parsed);
            case "info":
                return await InfoAsync(ctx);
            case null:
                Console.Error.WriteLine(Help);
                return 2;
            default:
                Console.Error.WriteLine($"Неизвестная подкоманда: {sub}");
                Console.Error.WriteLine(Help);
                return 2;
        }
    }

    private static async Task<int> ScanAsync(CommandContext ctx)
    {
        if (!ctx.JsonOutput)
        {
            Formatter.WriteLineColored("Сканирование Wi-Fi…", ConsoleColor.DarkGray);
        }

        var json = await ctx.Gateway.GetNetworkScanAsync(ctx.Token);

        if (ctx.JsonOutput)
        {
            Console.WriteLine(json.GetRawText());
            return 0;
        }

        var networks = new List<NetworkScanDto>();
        if (json.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in json.EnumerateArray())
            {
                var net = item.Deserialize<NetworkScanDto>(ctx.Json);
                if (net is not null)
                {
                    networks.Add(net);
                }
            }
        }

        var headers = new[] { "#", "SSID", "BSSID", "Сигнал", "Защита" };
        var rows = networks.Select((n, i) => new[]
        {
            (i + 1).ToString(),
            string.IsNullOrEmpty(n.Ssid) ? (n.Hidden ? "<скрытая>" : "<неизвестно>") : n.Ssid!,
            n.Bssid ?? "—",
            $"{n.SignalQuality}%",
            n.AuthName,
        }).ToList();

        Formatter.Table(headers, rows);
        return 0;
    }

    private static async Task<int> SetAsync(CommandContext ctx, Args parsed)
    {
        var root = new JsonObject();

        if (parsed.Get("hostname") is { } hostname)
        {
            root["hostname"] = hostname;
        }

        if (parsed.HasFlag("use-dhcp"))
        {
            root["useDhcp"] = parsed.IsSet("use-dhcp");
        }

        var staticConfig = new JsonObject();
        AddIfPresent(staticConfig, "ip", parsed.Get("static-ip"));
        AddIfPresent(staticConfig, "gateway", parsed.Get("gateway"));
        AddIfPresent(staticConfig, "subnet", parsed.Get("subnet"));
        AddIfPresent(staticConfig, "dns", parsed.Get("dns"));
        if (staticConfig.Count > 0)
        {
            root["staticConfig"] = staticConfig;
        }

        if (root.Count == 0)
        {
            Console.Error.WriteLine("Не задано ни одного параметра. См. otg network set --help.");
            return 2;
        }

        using var doc = JsonDocument.Parse(root.ToJsonString());
        await ctx.Gateway.PostNetworkSettingsAsync(doc.RootElement.Clone(), ctx.Token);
        Formatter.WriteLineColored("Сетевые настройки отправлены.", ConsoleColor.Green);
        return 0;
    }

    private static void AddIfPresent(JsonObject obj, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            obj[key] = value;
        }
    }

    private static async Task<int> InfoAsync(CommandContext ctx)
    {
        var vars = await ctx.Gateway.GetVarsAsync(ctx.Token);

        if (ctx.JsonOutput)
        {
            Console.WriteLine(vars.GetRawText());
            return 0;
        }

        var dto = vars.Deserialize<VarsDto>(ctx.Json);
        var connected = dto?.Master?.Network?.Connected;
        Console.Write("Подключено к Wi-Fi: ");
        Formatter.WriteLineColored(
            connected == true ? "да" : "нет",
            connected == true ? ConsoleColor.Green : ConsoleColor.Red);
        return 0;
    }
}
