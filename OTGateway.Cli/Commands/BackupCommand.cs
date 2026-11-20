using OTGateway.Cli.Helpers;

namespace OTGateway.Cli.Commands;

/// <summary>
/// otg backup &lt;save|restore&gt; [файл] — сохранение/восстановление настроек.
/// </summary>
public static class BackupCommand
{
    /// <summary>Нижняя и верхняя границы длины тела restore, которые принимает прошивка (src/PortalTask.h).</summary>
    public const int MinRestoreBytes = 5;
    public const int MaxRestoreBytes = 2536;

    public const string Help = """
        Использование: otg backup <команда> [файл]
          save [file.json]        скачать резервную копию настроек
                                  (по умолчанию otgateway-backup-<timestamp>.json)
          restore <file.json> [--yes]  восстановить настройки из файла
        """;

    public static async Task<int> RunAsync(CommandContext ctx, string[] args)
    {
        var parsed = Args.Parse(args);
        var sub = parsed[0];

        switch (sub)
        {
            case "save":
                return await SaveAsync(ctx, parsed[1]);
            case "restore":
                return await RestoreAsync(ctx, parsed[1]);
            case null:
                Console.Error.WriteLine(Help);
                return 2;
            default:
                Console.Error.WriteLine($"Неизвестная подкоманда: {sub}");
                Console.Error.WriteLine(Help);
                return 2;
        }
    }

    private static async Task<int> SaveAsync(CommandContext ctx, string? file)
    {
        var target = string.IsNullOrWhiteSpace(file)
            ? $"otgateway-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json"
            : file!;

        var bytes = await ctx.Gateway.DownloadBackupAsync(ctx.Token);
        await File.WriteAllBytesAsync(target, bytes, ctx.Token);
        Formatter.WriteLineColored($"Резервная копия сохранена: {target} ({bytes.Length} байт).", ConsoleColor.Green);
        return 0;
    }

    private static async Task<int> RestoreAsync(CommandContext ctx, string? file)
    {
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            Console.Error.WriteLine($"Файл не найден: {file}");
            return 2;
        }

        var bytes = await File.ReadAllBytesAsync(file, ctx.Token);

        // Проверяем локально до отправки: валидность JSON и лимит размера тела устройства.
        var error = ValidateRestorePayload(bytes);
        if (error is not null)
        {
            Formatter.WriteColored("Ошибка: ", ConsoleColor.Red);
            Console.Error.WriteLine(error);
            return 2;
        }

        if (!ctx.AssumeYes && !ctx.Confirm($"Восстановить настройки из {file}? Текущие будут перезаписаны."))
        {
            Formatter.WriteLineColored("Отменено.", ConsoleColor.DarkGray);
            return 1;
        }

        await ctx.Gateway.UploadBackupAsync(bytes, ctx.Token);
        // При изменении настроек прошивка ставит vars.actions.restart → устройство перезагружается.
        Formatter.WriteLineColored(
            "Настройки восстановлены. Устройство будет перезагружено.",
            ConsoleColor.Green);
        return 0;
    }

    /// <summary>
    /// Локальная проверка тела перед POST /api/backup/restore. Возвращает текст ошибки или null, если всё ок.
    /// Прошивка (src/PortalTask.h) отвергает тела длиннее 2536 байт (HTTP 413) и короче 5 байт (406),
    /// а также невалидный JSON (400) — ловим это заранее с понятным сообщением.
    /// </summary>
    public static string? ValidateRestorePayload(byte[] bytes)
    {
        if (bytes is null || bytes.Length < MinRestoreBytes)
        {
            return $"Файл слишком маленький ({bytes?.Length ?? 0} байт): пустой или не является резервной копией.";
        }

        if (bytes.Length > MaxRestoreBytes)
        {
            return $"Файл слишком большой ({bytes.Length} байт). Устройство принимает резервные копии не более {MaxRestoreBytes} байт " +
                   "(ограничение /api/backup/restore).";
        }

        try
        {
            using var _ = System.Text.Json.JsonDocument.Parse(bytes);
        }
        catch (System.Text.Json.JsonException ex)
        {
            return $"Файл не является корректным JSON: {ex.Message}";
        }

        return null;
    }
}
