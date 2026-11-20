using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OTGateway.Cli.Helpers;

namespace OTGateway.Cli.Services;

/// <summary>
/// HTTP-клиент для JSON API OTGateway (все эндпоинты под http://&lt;address&gt;/api/).
/// Поддерживает необязательную HTTP Basic авторизацию.
/// </summary>
public sealed class GatewayClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly ILogger<GatewayClient> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _scanBudget;
    private readonly TimeSpan _scanPollInterval = TimeSpan.FromSeconds(3);

    public string BaseUrl { get; }

    public GatewayClient(
        string baseUrl,
        string? username,
        string? password,
        int timeoutSeconds,
        ILogger<GatewayClient> logger,
        int scanBudgetSeconds = 60,
        TimeProvider? timeProvider = null,
        string? proxy = null)
        : this(baseUrl, username, password, timeoutSeconds, logger, ProxyFactory.CreateHandler(proxy), scanBudgetSeconds, timeProvider)
    {
    }

    /// <summary>Конструктор с инъекцией обработчика (для тестов).</summary>
    public GatewayClient(
        string baseUrl,
        string? username,
        string? password,
        int timeoutSeconds,
        ILogger<GatewayClient> logger,
        HttpMessageHandler handler,
        int scanBudgetSeconds = 60,
        TimeProvider? timeProvider = null)
    {
        BaseUrl = baseUrl.TrimEnd('/');
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _scanBudget = TimeSpan.FromSeconds(scanBudgetSeconds > 0 ? scanBudgetSeconds : 60);

        _http = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(BaseUrl + "/"),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds),
        };

        var auth = BuildAuthorizationHeader(username, password);
        if (auth is not null)
        {
            _http.DefaultRequestHeaders.Authorization = auth;
        }
    }

    /// <summary>Собирает Basic-auth заголовок из логина/пароля. Возвращает null, если учётных данных нет.</summary>
    public static AuthenticationHeaderValue? BuildAuthorizationHeader(string? username, string? password)
    {
        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
        {
            return null;
        }

        var raw = $"{username}:{password}";
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        return new AuthenticationHeaderValue("Basic", token);
    }

    private static string Describe(HttpMethod method, string path) => $"{method} {path}";

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.ToString() ?? "?";
        _logger.LogDebug("→ {Desc}", Describe(request.Method, path));

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GatewayException(
                $"Превышено время ожидания ответа от {BaseUrl} (таймаут). Проверьте доступность устройства.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new GatewayException(
                $"Не удалось связаться с {BaseUrl}: {ex.Message}", ex);
        }

        return response;
    }

    private async Task<string> ReadBodySafeAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>Бросает GatewayException при неуспешном HTTP-статусе, иначе возвращает тело ответа.</summary>
    private async Task<string> EnsureSuccessAsync(HttpResponseMessage response, string path, CancellationToken ct)
    {
        var body = await ReadBodySafeAsync(response, ct);
        if (!response.IsSuccessStatusCode)
        {
            var status = (int)response.StatusCode;
            var detail = string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase ?? "" : body.Trim();
            throw new GatewayException(
                $"Ошибка API ({status}): {detail}",
                $"{BaseUrl}/{path}",
                response.StatusCode,
                body);
        }

        _logger.LogDebug("← {Status} {Path} ({Len} байт)", (int)response.StatusCode, path, body.Length);
        return body;
    }

    private async Task<JsonElement> GetJsonAsync(string path, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await SendAsync(request, ct);
        var body = await EnsureSuccessAsync(response, path, ct);
        return ParseJson(body, path);
    }

    private async Task<JsonElement> PostJsonAsync(string path, string json, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        using var response = await SendAsync(request, ct);
        var body = await EnsureSuccessAsync(response, path, ct);
        return string.IsNullOrWhiteSpace(body) ? default : ParseJson(body, path);
    }

    private JsonElement ParseJson(string body, string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "null" : body);
            return doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new GatewayException(
                $"Некорректный JSON в ответе {path}: {ex.Message}", $"{BaseUrl}/{path}", null, body);
        }
    }

    public Task<JsonElement> GetVarsAsync(CancellationToken ct = default) =>
        GetJsonAsync("api/vars", ct);

    public Task PostVarsAsync(object payload, CancellationToken ct = default) =>
        PostJsonAsync("api/vars", JsonSerializer.Serialize(payload, JsonConfig.Writing), ct);

    public Task<JsonElement> GetSettingsAsync(CancellationToken ct = default) =>
        GetJsonAsync("api/settings", ct);

    public Task<JsonElement> PostSettingsAsync(JsonElement patch, CancellationToken ct = default) =>
        PostJsonAsync("api/settings", patch.GetRawText(), ct);

    public Task<JsonElement> GetSensorsAsync(bool detailed, CancellationToken ct = default) =>
        GetJsonAsync(detailed ? "api/sensors?detailed=1" : "api/sensors", ct);

    public Task PostSensorAsync(string id, JsonElement payload, CancellationToken ct = default) =>
        PostJsonAsync($"api/sensor?id={Uri.EscapeDataString(id)}", payload.GetRawText(), ct);

    public Task<JsonElement> GetNetworkScanAsync(CancellationToken ct = default) =>
        PollNetworkScanAsync(ct);

    /// <summary>
    /// Сканирование Wi-Fi на устройстве асинхронное. Пока поиск сетей идёт (или ещё не завершён),
    /// прошивка отдаёт <c>404 Not Found</c> и запускает <c>WiFi.scanNetworks()</c>.
    /// Поэтому <c>404</c>, <c>202</c> и пустое тело считаем «сканирование в процессе» и опрашиваем дальше
    /// каждые <see cref="_scanPollInterval"/> до истечения <see cref="_scanBudget"/>.
    /// Ошибки авторизации (401/403) и прочие HTTP-ошибки пробрасываем сразу.
    /// </summary>
    private async Task<JsonElement> PollNetworkScanAsync(CancellationToken ct)
    {
        const string path = "api/network/scan";
        var deadline = _timeProvider.GetUtcNow() + _scanBudget;

        while (true)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            using var response = await SendAsync(request, ct);
            var body = await ReadBodySafeAsync(response, ct);

            // 404/202/пустое тело → сканирование ещё идёт.
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Accepted
                || (response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(body)))
            {
                _logger.LogInformation("Сканирование ещё идёт…");
            }
            else if (response.IsSuccessStatusCode)
            {
                return ParseJson(body, path);
            }
            else
            {
                var status = (int)response.StatusCode;
                var detail = string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase ?? "" : body.Trim();
                throw new GatewayException($"Ошибка API ({status}): {detail}", $"{BaseUrl}/{path}", response.StatusCode, body);
            }

            if (_timeProvider.GetUtcNow() >= deadline)
            {
                throw new GatewayException(
                    $"Превышено время ожидания завершения сканирования Wi-Fi ({_scanBudget.TotalSeconds:N0} с).",
                    $"{BaseUrl}/{path}",
                    null,
                    body);
            }

            await DelayScanAsync(ct);
        }
    }

    /// <summary>Пауза между опросами сканирования через <see cref="TimeProvider"/> (для тестов с фейковым временем).</summary>
    private async Task DelayScanAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var timer = _timeProvider.CreateTimer(_ => tcs.TrySetResult(), null, _scanPollInterval, Timeout.InfiniteTimeSpan);
        await using var registration = ct.Register(() => tcs.TrySetCanceled(ct));
        await tcs.Task.ConfigureAwait(false);
    }

    public Task<JsonElement> PostNetworkSettingsAsync(JsonElement payload, CancellationToken ct = default) =>
        PostJsonAsync("api/network/settings", payload.GetRawText(), ct);

    public async Task<byte[]> DownloadBackupAsync(CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/backup/save");
        using var response = await SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            await EnsureSuccessAsync(response, "api/backup/save", ct);
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task UploadBackupAsync(byte[] json, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/backup/restore")
        {
            Content = new ByteArrayContent(json),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var response = await SendAsync(request, ct);
        await EnsureSuccessAsync(response, "api/backup/restore", ct);
    }

    /// <summary>
    /// Сведения о версии. Эндпоинта <c>/api/version</c> на устройстве нет; build/version
    /// отдаёт <c>GET /api/info</c> в поле <c>build.version</c> (плюс chip/build/heap/flash).
    /// </summary>
    public Task<JsonElement> GetVersionAsync(CancellationToken ct = default) =>
        GetJsonAsync("api/info", ct);

    /// <summary>
    /// Перезагрузка устройства. Отдельного <c>/api/restart</c> нет — перезапуск задаётся действием
    /// <c>POST /api/vars {"actions":{"restart":true}}</c>. Устройство уходит в ребут и может разорвать
    /// соединение до ответа — это допустимо, не считаем ошибкой.
    /// </summary>
    public async Task RestartAsync(CancellationToken ct = default)
    {
        const string path = "api/vars";
        const string body = """{"actions":{"restart":true}}""";

        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        try
        {
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
            // 503 — устройство уже перезагружается (vars.states.restarting) — тоже нормально.
            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.ServiceUnavailable)
            {
                await EnsureSuccessAsync(response, path, ct);
            }
        }
        catch (HttpRequestException)
        {
            // Соединение разорвано при перезагрузке — ожидаемо.
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            // Таймаут/разрыв сокета из-за ребута, а не отмена пользователем — ожидаемо.
        }
    }

    public void Dispose() => _http.Dispose();
}
