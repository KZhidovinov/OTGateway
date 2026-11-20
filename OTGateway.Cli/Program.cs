using System.Text;
using Microsoft.Extensions.Logging;
using OTGateway.Cli.Commands;
using OTGateway.Cli.Helpers;
using OTGateway.Cli.Services;

namespace OTGateway.Cli;

public static class Program
{
    private static readonly CancellationTokenSource Cts = new();

    /// <summary>Названия переменных окружения для учётных данных/адреса (см. README).</summary>
    public const string EnvAddress = "OTG_ADDRESS";
    public const string EnvUser = "OTG_USER";
    public const string EnvPassword = "OTG_PASSWORD";
    public const string EnvToken = "OTG_TOKEN";
    public const string EnvProxy = "OTG_PROXY";

    public static async Task<int> Main(string[] rawArgs)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Cts.Cancel();
        };

        if (rawArgs.Length == 0 || rawArgs[0] is "--help" or "-h" or "help")
        {
            PrintRootHelp();
            return rawArgs.Length == 0 ? 2 : 0;
        }

        var (options, command, commandArgs, error) = ParseGlobal(rawArgs);
        if (error is not null)
        {
            Console.Error.WriteLine(error);
            return 2;
        }

        if (command is null)
        {
            PrintRootHelp();
            return 2;
        }

        return await RunAsync(options, command, commandArgs);
    }

    private sealed record Options(
        string? Address,
        int Port,
        string? Username,
        string? Password,
        int TimeoutSeconds,
        bool TimeoutProvided,
        bool Json,
        bool Yes,
        bool Verbose,
        string? Proxy);

    private static (Options, string? Command, string[] Args, string? Error) ParseGlobal(string[] raw)
    {
        string? address = null;
        var port = 80;
        string? username = null;
        string? password = null;
        var timeout = 30;
        var timeoutProvided = false;
        var json = false;
        var yes = false;
        var verbose = false;
        string? proxy = null;

        var command = (string?)null;
        var commandArgs = Array.Empty<string>();

        var i = 0;
        // Сначала разгребаем глобальные флаги до имени команды.
        for (; i < raw.Length; i++)
        {
            var arg = raw[i];
            switch (arg)
            {
                case "--address" or "-a":
                    address = Next(raw, ref i);
                    break;
                case "--port":
                    if (!int.TryParse(Next(raw, ref i), out port))
                    {
                        return (null!, null, null!, "Некорректное значение --port.");
                    }

                    break;
                case "--user":
                    username = Next(raw, ref i);
                    break;
                case "--password":
                    password = Next(raw, ref i);
                    break;
                case "--token":
                    // Формат "login:password".
                    var token = Next(raw, ref i) ?? string.Empty;
                    var colon = token.IndexOf(':');
                    if (colon >= 0)
                    {
                        username = token[..colon];
                        password = token[(colon + 1)..];
                    }
                    else
                    {
                        username = token;
                    }

                    break;
                case "--timeout":
                    if (!int.TryParse(Next(raw, ref i), out timeout))
                    {
                        return (null!, null, null!, "Некорректное значение --timeout.");
                    }

                    timeoutProvided = true;
                    break;
                case "--proxy":
                    proxy = Next(raw, ref i);
                    break;
                case "--json":
                    json = true;
                    break;
                case "--yes" or "-y":
                    yes = true;
                    break;
                case "--verbose" or "-v":
                    verbose = true;
                    break;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        return (null!, null, null!, $"Неизвестный глобальный параметр: {arg}");
                    }

                    // Это имя команды — остальное аргументы команды.
                    command = arg;
                    commandArgs = raw[(i + 1)..];
                    i = raw.Length; // выход из цикла
                    break;
            }
        }

        var options = new Options(address, port, username, password, timeout, timeoutProvided, json, yes, verbose, proxy);

        // Булевы глобальные флаги (--json/--yes/--verbose) принимаем в любой позиции, в т.ч. после команды.
        if (command is not null)
        {
            var kept = new List<string>();
            foreach (var a in commandArgs)
            {
                switch (a)
                {
                    case "--json":
                        json = true;
                        break;
                    case "--yes" or "-y":
                        yes = true;
                        break;
                    case "--verbose" or "-v":
                        verbose = true;
                        break;
                    default:
                        kept.Add(a);
                        break;
                }
            }

            commandArgs = kept.ToArray();
            options = options with { Json = json, Yes = yes, Verbose = verbose };
        }

        return (options, command, commandArgs, null);
    }

    private static string? Next(string[] raw, ref int i)
    {
        if (i + 1 >= raw.Length)
        {
            return null;
        }

        i++;
        return raw[i];
    }

    private static async Task<int> RunAsync(Options options, string command, string[] commandArgs)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSimpleConsole(o =>
            {
                o.SingleLine = true;
                o.TimestampFormat = "HH:mm:ss ";
            });
            builder.SetMinimumLevel(options.Verbose ? LogLevel.Debug : LogLevel.Warning);
        });

        // Учётные данные/адрес: CLI-флаги главнее переменных окружения, окружение — резерв.
        var (address, username, password) = ResolveAuthOptions(
            options.Address, options.Username, options.Password, Environment.GetEnvironmentVariable);
        options = options with { Address = address, Username = username, Password = password };

        // Прокси: флаг --proxy главнее OTG_PROXY.
        var proxy = options.Proxy ?? Environment.GetEnvironmentVariable(EnvProxy);
        options = options with { Proxy = string.IsNullOrWhiteSpace(proxy) ? null : proxy };

        if (!ProxyFactory.TryValidate(options.Proxy, out var proxyError))
        {
            Console.Error.WriteLine(proxyError);
            return 2;
        }

        // Команды, которым не нужен адрес устройства.
        if (command is "help")
        {
            PrintRootHelp();
            return 0;
        }

        if (string.IsNullOrWhiteSpace(options.Address))
        {
            Console.Error.WriteLine($"Команда «{command}» требует параметр --address <ip|host> или переменную окружения {EnvAddress}.");
            return 2;
        }

        var baseUrl = BuildBaseUrl(options.Address, options.Port);

        if (HasCredentials(username, password) &&
            !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine(
                "Предупреждение: Basic-авторизация поверх незащищённого HTTP — логин/пароль идут открытым текстом (приемлемо только в доверенной LAN).");
        }

        try
        {
        var gwLogger = loggerFactory.CreateLogger<GatewayClient>();

        // Бюджет ожидания завершения сканирования Wi-Fi: из --timeout либо 60 с по умолчанию.
        var scanBudget = options.TimeoutProvided ? options.TimeoutSeconds : 60;

        using var gateway = new GatewayClient(baseUrl, username, password, options.TimeoutSeconds, gwLogger, scanBudget, proxy: options.Proxy);
        var ctx = new CommandContext
        {
            Gateway = gateway,
            LoggerFactory = loggerFactory,
            Json = JsonConfig.Reading,
            Address = options.Address,
            Port = options.Port,
            Username = username,
            Password = password,
            TimeoutSeconds = options.TimeoutSeconds,
            JsonOutput = options.Json,
            AssumeYes = options.Yes,
            Verbose = options.Verbose,
            Token = Cts.Token,
            Confirm = AskConfirm,
            Proxy = options.Proxy,
        };

        return command switch
        {
            "status" => await StatusCommand.RunAsync(ctx, commandArgs),
            "settings" => await SettingsCommand.RunAsync(ctx, commandArgs),
            "sensor" => await SensorCommand.RunAsync(ctx, commandArgs),
            "network" => await NetworkCommand.RunAsync(ctx, commandArgs),
            "backup" => await BackupCommand.RunAsync(ctx, commandArgs),
            "upgrade" => await UpgradeCommand.RunAsync(ctx, commandArgs),
            "version" => await VersionCommand.RunAsync(ctx, commandArgs),
            "restart" => await RestartCommand.RunAsync(ctx, commandArgs),
            _ => Unknown(command),
        };
        }
        catch (UriFormatException ex)
        {
            Formatter.WriteColored("Некорректный адрес: ", ConsoleColor.Red);
            Console.WriteLine($"{baseUrl} ({ex.Message})");
            return 2;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Прервано пользователем.");
            return 1;
        }
        catch (GatewayException ex)
        {
            Formatter.WriteColored("Ошибка API: ", ConsoleColor.Red);
            Console.WriteLine(ex.Message);
            if (ex.Url is not null)
            {
                Console.Error.WriteLine($"  URL: {ex.Url}");
            }

            if (ex.StatusCode is { } status)
            {
                Console.Error.WriteLine($"  HTTP статус: {(int)status} {status}");
            }

            if (options.Verbose && !string.IsNullOrWhiteSpace(ex.ResponseBody))
            {
                Console.Error.WriteLine($"  Тело ответа: {ex.ResponseBody}");
            }

            return 1;
        }
        catch (HttpRequestException ex)
        {
            Formatter.WriteColored("Сетевая ошибка: ", ConsoleColor.Red);
            Console.WriteLine(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            Formatter.WriteColored("Ошибка: ", ConsoleColor.Red);
            Console.WriteLine(ex.Message);
            if (options.Verbose)
            {
                Console.Error.WriteLine(ex);
            }

            return 1;
        }
    }

    private static string BuildBaseUrl(string address, int port)
    {
        // Уже с схемой — используем как есть.
        if (address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return address.TrimEnd('/');
        }

        // Адрес уже содержит порт (host:port) — не добавляем свой.
        // Отличаем IPv6 в скобках и голый host.
        var hostPart = address;
        var hasPort = false;
        if (hostPart.StartsWith("[", StringComparison.Ordinal))
        {
            var close = hostPart.IndexOf(']');
            if (close > 0 && close + 1 < hostPart.Length && hostPart[close + 1] == ':')
            {
                hasPort = true;
            }
        }
        else
        {
            hasPort = hostPart.IndexOf(':') >= 0;
        }

        return hasPort ? $"http://{address}" : $"http://{address}:{port}";
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Неизвестная команда: {command}");
        PrintRootHelp();
        return 2;
    }

    private static bool AskConfirm(string prompt)
    {
        Formatter.WriteColored($"{prompt} [y/N] ", ConsoleColor.Yellow);
        var answer = Console.ReadLine();
        return answer is not null && (answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) ||
                                      answer.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Есть ли какие-то учётные данные для Basic-auth.</summary>
    private static bool HasCredentials(string? user, string? password) =>
        !string.IsNullOrEmpty(user) || !string.IsNullOrEmpty(password);

    /// <summary>
    /// Разрешает адрес и учётные данные: переданные CLI-флаги имеют приоритет, для отсутствующих
    /// подставляются переменные окружения. Порядок для env учётки: сначала парная <c>{EnvUser}</c>/<c>{EnvPassword}</c>,
    /// затем (если ничего нет) единый токен <c>{EnvToken}</c> вида «login:password».
    /// </summary>
    public static (string? Address, string? User, string? Password) ResolveAuthOptions(
        string? address,
        string? user,
        string? password,
        Func<string, string?> getenv)
    {
        address ??= getenv(EnvAddress);
        user ??= getenv(EnvUser);
        password ??= getenv(EnvPassword);

        if (string.IsNullOrEmpty(user) && string.IsNullOrEmpty(password))
        {
            var token = getenv(EnvToken);
            if (!string.IsNullOrEmpty(token))
            {
                var colon = token.IndexOf(':');
                if (colon >= 0)
                {
                    user = token[..colon];
                    password = token[(colon + 1)..];
                }
                else
                {
                    user = token;
                }
            }
        }

        return (address, user, password);
    }

    private static void PrintRootHelp()
    {
        Console.WriteLine("""
            otg — CLI-клиент для OTGateway (контроллер газовых котлов OpenTherm)

            Использование:
              otg [глобальные параметры] <команда> [аргументы команды]

            Глобальные параметры:
              -a, --address <ip|host>  адрес устройства (обязателен для большинства команд)
                  --port <num>         HTTP-порт (по умолчанию 80)
                  --user <login>       имя пользователя для Basic-авторизации
                  --password <pass>    пароль
                  --token <login:pass> учётные данные одной строкой
                  --timeout <sec>      таймаут HTTP-запросов (по умолчанию 30)
                  --proxy <url>        прокси: socks5://host:port или http://host:port
                  --json               выводить сырой JSON
                  -y, --yes            без интерактивных подтверждений
              -v, --verbose            подробное логирование (Debug)
              -h, --help               эта справка

            Переменные окружения (подставляются, если соответствующий флаг не задан; флаги главнее):
              OTG_ADDRESS, OTG_USER, OTG_PASSWORD, OTG_TOKEN=login:password, OTG_PROXY=socks5://host:port

            Команды:
              status     состояние шлюза и котла                 [--watch <sec>]
              settings   работа с настройками     read|write|set|reset-fault|reset-diag
              sensor     датчики                                   list|set|enable|disable
              network    Wi-Fi: сканирование и настройки          scan|set|info
              backup     резервные копии настроек                save|restore
              upgrade    обновление прошивки/файловой системы      fw|fs
              version    версия прошивки
              restart    перезагрузка устройства

            Примеры:
              otg --address 192.168.1.50 status
              otg -a 192.168.1.50 --user admin --password s3cret settings read
              otg -a 192.168.1.50 settings set heating.target 55
              otg -a 192.168.1.50 network scan
              otg -a 192.168.1.50 backup save config.json
            """);
    }
}
