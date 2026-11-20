using System.Text.Json;
using OTGateway.Cli.Helpers;
using OTGateway.Cli.Models;

namespace OTGateway.Cli.Commands;

/// <summary>
/// otg status [--watch &lt;сек&gt;] — состояние шлюза и котла из GET /api/vars.
/// </summary>
public static class StatusCommand
{
    private static readonly Dictionary<int, string> VendorByMemberId = new()
    {
        [1] = "Baxi",
        [2] = "AWB/Brink/Viessmann",
        [4] = "ATAG/Baxi/Brötje/ELCO/GEMINOX",
        [5] = "Itho Daalderop",
        [6] = "IDEAL",
        [8] = "Buderus/Bosch/Hoval",
        [9] = "Ferroli",
        [11] = "Remeha/De Dietrich",
        [16] = "Unical",
        [24] = "Vaillant/Bulex",
        [27] = "Baxi",
        [29] = "Itho Daalderop",
        [33] = "Viessmann",
        [41] = "Italtherm/Radiant",
        [56] = "Baxi",
        [131] = "Nefit",
        [148] = "Navien",
        [173] = "Intergas",
        [247] = "Baxi Ampera",
        [248] = "Zota Lux-X",
    };

    public static string VendorName(int? memberId) =>
        memberId is int id && VendorByMemberId.TryGetValue(id, out var name) ? name : "unknown";

    public static async Task<int> RunAsync(CommandContext ctx, string[] args)
    {
        var parsed = Args.Parse(args);
        var watch = parsed.GetInt("watch", 0);
        var ct = ctx.Token;

        do
        {
            var vars = await ctx.Gateway.GetVarsAsync(ct);

            if (ctx.JsonOutput)
            {
                Console.WriteLine(vars.GetRawText());
            }
            else
            {
                Print(vars.Deserialize<VarsDto>(ctx.Json));
            }

            if (watch <= 0)
            {
                break;
            }

            Console.WriteLine();
            Formatter.WriteLineColored($"Обновление каждые {watch} с · Ctrl+C для выхода", ConsoleColor.DarkGray);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(watch), ct);
                Console.Clear();
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        while (!ct.IsCancellationRequested);

        return 0;
    }

    private static void Print(VarsDto? dto)
    {
        var master = dto?.Master;
        var slave = dto?.Slave;
        var heating = master?.Heating;
        var dhw = master?.Dhw;

        Section("Подключение");
        Bool("Wi-Fi", master?.Network?.Connected);
        Bool("MQTT", master?.Mqtt?.Connected);
        Bool("Котёл", slave?.Connected);
        Value("Пламя", slave?.Flame == true ? ("активно", ConsoleColor.Green) : ("нет", ConsoleColor.DarkGray));

        Section("Отопление");
        Bool("Вкл", heating?.Enabled);
        Value("Уставка", Plain(Formatter.Temp(heating?.TargetTemp)));
        Value("Setpoint", Plain(Formatter.Temp(heating?.SetpointTemp)));
        Value("Текущая", Plain(Formatter.Temp(heating?.CurrentTemp)));
        Value("Обратка", Plain(Formatter.Temp(heating?.ReturnTemp)));

        Section("ГВС");
        Bool("Вкл", dhw?.Enabled);
        Value("Уставка", Plain(Formatter.Temp(dhw?.TargetTemp)));
        Value("Текущая", Plain(Formatter.Temp(dhw?.CurrentTemp)));

        Section("Котёл (OpenTherm)");
        Value("Производитель", Plain($"{VendorName(slave?.MemberId)} (id={slave?.MemberId?.ToString() ?? "—"})"));

        var fault = slave?.Fault;
        Value("Fault", fault?.Active == true
            ? ($"активно, код 0x{(fault.Code ?? 0):X2}", ConsoleColor.Red)
            : ("нет", ConsoleColor.DarkGray));

        var diag = slave?.Diag;
        Value("Diag", diag?.Active == true
            ? ($"активно, код 0x{(diag.Code ?? 0):X2}", ConsoleColor.Yellow)
            : ("нет", ConsoleColor.DarkGray));
    }

    private static void Section(string title) => Formatter.WriteLineColored($"── {title} ──", ConsoleColor.DarkCyan);

    private static void Bool(string label, bool? value)
    {
        var color = value switch { true => ConsoleColor.Green, false => ConsoleColor.DarkGray, _ => ConsoleColor.DarkGray };
        Value(label, (Formatter.Bool(value), color));
    }

    private static (string, ConsoleColor) Plain(string text) => (text, ConsoleColor.Gray);

    private static void Value(string label, (string Text, ConsoleColor Color) value)
    {
        Console.Write(label.PadRight(14));
        Formatter.WriteLineColored(value.Text, value.Color);
    }
}
