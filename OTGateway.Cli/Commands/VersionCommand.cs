using OTGateway.Cli.Helpers;

namespace OTGateway.Cli.Commands;

/// <summary>
/// otg version — сведения о версии прошивки из GET /api/info (поле build.version, плюс chip/build/heap).
/// </summary>
public static class VersionCommand
{
    public const string Help = """
        Использование: otg version [--json]
          --json   вывести сырой JSON ответа
        """;

    public static async Task<int> RunAsync(CommandContext ctx, string[] args)
    {
        var info = await ctx.Gateway.GetVersionAsync(ctx.Token);

        if (ctx.JsonOutput)
        {
            Console.WriteLine(info.GetRawText());
            return 0;
        }

        JsonExtensions.PrintJsonTree(info);
        return 0;
    }
}
