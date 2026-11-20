using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OTGateway.Cli.Helpers;
using OTGateway.Cli.Models;

namespace OTGateway.Cli.Services;

/// <summary>
/// Клиент загрузки прошивки/файловой системы через POST /api/upgrade (multipart/form-data).
/// </summary>
public sealed class UpgradeClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly ILogger<UpgradeClient> _logger;

    public UpgradeClient(GatewayClient gateway, string? username, string? password, ILogger<UpgradeClient> logger)
        : this(gateway.BaseUrl, username, password, logger, new HttpClientHandler())
    {
    }

    /// <summary>Конструктор с инъекцией обработчика (для тестов).</summary>
    public UpgradeClient(string baseUrl, string? username, string? password, ILogger<UpgradeClient> logger, HttpMessageHandler handler)
    {
        _logger = logger;
        _http = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromMinutes(10), // прошивка может долго писаться на флеш
        };

        var auth = GatewayClient.BuildAuthorizationHeader(username, password);
        if (auth is not null)
        {
            _http.DefaultRequestHeaders.Authorization = auth;
        }
    }

    public async Task<UpgradeResult> UpgradeAsync(
        Stream? firmware,
        string? firmwareName,
        Stream? filesystem,
        string? filesystemName,
        CancellationToken cancellationToken = default)
    {
        if (firmware is null && filesystem is null)
        {
            throw new ArgumentException("Нужен хотя бы один файл: firmware или filesystem.");
        }

        using var content = new MultipartFormDataContent();

        if (firmware is not null)
        {
            var fc = new StreamContent(firmware);
            fc.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fc, "firmware", firmwareName ?? "firmware.bin");
        }

        if (filesystem is not null)
        {
            var sc = new StreamContent(filesystem);
            sc.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(sc, "filesystem", filesystemName ?? "filesystem.bin");
        }

        _logger.LogDebug("→ POST api/upgrade (multipart)");

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsync("api/upgrade", content, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GatewayException("Превышено время ожидания ответа при обновлении.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new GatewayException($"Не удалось отправить обновление: {ex.Message}", ex);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var detail = string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase ?? "" : body.Trim();
                throw new GatewayException(
                    $"Ошибка обновления ({(int)response.StatusCode}): {detail}",
                    _http.BaseAddress + "api/upgrade",
                    response.StatusCode,
                    body);
            }

            return ParseResult(body);
        }
    }

    private static UpgradeResult ParseResult(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return new UpgradeResult(null, null);
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            UpgradeStatusResult? Read(string name) =>
                root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Object
                    ? el.Deserialize<UpgradeStatusResult>(JsonConfig.Reading)
                    : null;

            return new UpgradeResult(Read("firmware"), Read("filesystem"));
        }
        catch (JsonException ex)
        {
            throw new GatewayException($"Некорректный JSON в ответе обновления: {ex.Message}", null, null, body);
        }
    }

    public void Dispose() => _http.Dispose();
}

/// <summary>Итог обновления по компонентам.</summary>
public sealed record UpgradeResult(UpgradeStatusResult? Firmware, UpgradeStatusResult? Filesystem)
{
    public bool Success =>
        (Firmware is null || Firmware.Success) && (Filesystem is null || Filesystem.Success);

    public string Describe()
    {
        var sb = new StringBuilder();
        if (Firmware is not null)
        {
            sb.Append("firmware: ").Append(Firmware.StatusText);
            if (!string.IsNullOrWhiteSpace(Firmware.Error))
            {
                sb.Append(" (").Append(Firmware.Error).Append(')');
            }
        }

        if (Filesystem is not null)
        {
            if (sb.Length > 0)
            {
                sb.Append(", ");
            }

            sb.Append("filesystem: ").Append(Filesystem.StatusText);
            if (!string.IsNullOrWhiteSpace(Filesystem.Error))
            {
                sb.Append(" (").Append(Filesystem.Error).Append(')');
            }
        }

        return sb.Length == 0 ? "нет данных" : sb.ToString();
    }
}
