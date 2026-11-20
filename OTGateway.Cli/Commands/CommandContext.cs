using System.Text.Json;
using Microsoft.Extensions.Logging;
using OTGateway.Cli.Services;

namespace OTGateway.Cli.Commands;

/// <summary>
/// Контекст команды: клиенты к шлюзу, фабрика логгеров, JSON-опции, глобальные флаги,
/// токен отмены приложения и делегат интерактивного подтверждения.
/// </summary>
public sealed class CommandContext
{
    public required GatewayClient Gateway { get; init; }
    public required ILoggerFactory LoggerFactory { get; init; }
    public required JsonSerializerOptions Json { get; init; }

    public string Address { get; init; } = string.Empty;
    public int Port { get; init; } = 80;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>--json: печатать сырой JSON вместо форматированного вывода.</summary>
    public bool JsonOutput { get; init; }

    /// <summary>--yes: без интерактивных подтверждений.</summary>
    public bool AssumeYes { get; init; }

    public bool Verbose { get; init; }

    /// <summary>--proxy / OTG_PROXY: URL прокси (socks5://host:port либо http://host:port), null — без прокси.</summary>
    public string? Proxy { get; init; }

    /// <summary>Токен остановки приложения (Ctrl+C). Команды используют его вместо статического Program.</summary>
    public CancellationToken Token { get; init; }

    /// <summary>Запрос подтверждения у пользователя: prompt → true (согласен). По умолчанию — авто-«да».</summary>
    public Func<string, bool> Confirm { get; init; } = static _ => true;

    /// <summary>Создаёт UpgradeClient поверх того же адреса/учётных данных (с учётом --proxy).</summary>
    public UpgradeClient CreateUpgradeClient()
    {
        var logger = LoggerFactory.CreateLogger<UpgradeClient>();
        return new UpgradeClient(Gateway.BaseUrl, Username, Password, logger, ProxyFactory.CreateHandler(Proxy));
    }
}
