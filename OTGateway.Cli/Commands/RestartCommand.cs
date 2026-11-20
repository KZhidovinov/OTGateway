using OTGateway.Cli.Helpers;

namespace OTGateway.Cli.Commands;

/// <summary>
/// otg restart [--yes] — перезагрузка устройства действием POST /api/vars {"actions":{"restart":true}}.
/// </summary>
public static class RestartCommand
{
    public const string Help = """
        Использование: otg restart [--yes]
          --yes   без подтверждения
        """;

    public static async Task<int> RunAsync(CommandContext ctx, string[] args)
    {
        if (!ctx.AssumeYes && !ctx.Confirm("Перезагрузить устройство?"))
        {
            Formatter.WriteLineColored("Отменено.", ConsoleColor.DarkGray);
            return 1;
        }

        await ctx.Gateway.RestartAsync(ctx.Token);
        Formatter.WriteLineColored("Команда перезагрузки отправлена, устройство перезагружается…", ConsoleColor.Yellow);
        return 0;
    }
}
