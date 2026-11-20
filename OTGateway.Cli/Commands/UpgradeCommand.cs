using OTGateway.Cli.Helpers;
using OTGateway.Cli.Models;
using OTGateway.Cli.Services;

namespace OTGateway.Cli.Commands;

/// <summary>
/// otg upgrade &lt;fw|fs&gt; &lt;file.bin&gt; — обновление прошивки/файловой системы.
/// </summary>
public static class UpgradeCommand
{
    public const string Help = """
        Использование: otg upgrade <команда> <file.bin>
          fw <firmware.bin>      обновить прошивку микроконтроллера
          fs <filesystem.bin>    обновить раздел файловой системы (LittleFS)
        """;

    public static async Task<int> RunAsync(CommandContext ctx, string[] args)
    {
        var parsed = Args.Parse(args);
        var kind = parsed[0];
        var file = parsed[1];

        if (kind is null || file is null)
        {
            Console.Error.WriteLine(Help);
            return 2;
        }

        if (kind is not ("fw" or "fs"))
        {
            Console.Error.WriteLine($"Неизвестный тип: {kind}. Ожидается fw или fs.");
            return 2;
        }

        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"Файл не найден: {file}");
            return 2;
        }

        var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        using (stream)
        {
            Formatter.WriteLineColored($"Отправка {Path.GetFileName(file)}…", ConsoleColor.DarkGray);

            using var upgrade = ctx.CreateUpgradeClient();
            UpgradeResult result;
            if (kind == "fw")
            {
                result = await upgrade.UpgradeAsync(stream, Path.GetFileName(file), null, null, ctx.Token);
            }
            else
            {
                result = await upgrade.UpgradeAsync(null, null, stream, Path.GetFileName(file), ctx.Token);
            }

            return Report(ctx, result, kind);
        }
    }

    private static int Report(CommandContext ctx, UpgradeResult result, string kind)
    {
        var status = kind == "fw" ? result.Firmware : result.Filesystem;
        if (status is null)
        {
            Console.Error.WriteLine("Устройство не вернуло статус обновления.");
            return 1;
        }

        var color = status.Success ? ConsoleColor.Green : ConsoleColor.Red;
        Formatter.WriteColored($"{kind}: {status.StatusText}", color);
        Console.WriteLine();

        if (!string.IsNullOrWhiteSpace(status.Error))
        {
            Formatter.WriteLineColored($"  ошибка: {status.Error}", ConsoleColor.Red);
        }

        return status.Success ? 0 : 1;
    }
}
