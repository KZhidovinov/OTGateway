using System.Net;

namespace OTGateway.Cli.Services;

/// <summary>
/// Создание HTTP-обработчиков с поддержкой прокси: --proxy socks5://host:port (или http://host:port).
/// Без прокси возвращается обычный HttpClientHandler — поведение по умолчанию не меняется.
/// </summary>
public static class ProxyFactory
{
    /// <summary>
    /// Проверяет URL прокси: допускается пустое значение (без прокси) либо абсолютный
    /// адрес со схемой socks5/http/https. Возвращает false и текст ошибки для некорректного ввода.
    /// </summary>
    public static bool TryValidate(string? proxy, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(proxy))
        {
            return true;
        }

        if (!Uri.TryCreate(proxy, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("socks5" or "http" or "https") ||
            string.IsNullOrEmpty(uri.Host))
        {
            error = $"Некорректный --proxy: «{proxy}». Ожидается socks5://host:port (например socks5://127.0.0.1:1080).";
            return false;
        }

        return true;
    }

    /// <summary>Создаёт обработчик HTTP: прямой (без прокси) либо через SOCKS5/HTTP-прокси.</summary>
    public static HttpMessageHandler CreateHandler(string? proxy)
    {
        if (string.IsNullOrWhiteSpace(proxy))
        {
            return new HttpClientHandler();
        }

        var uri = new Uri(proxy);
        var webProxy = new WebProxy(uri);

        // socks5://user:pass@host:port — учётные данные из UserInfo (SOCKS5 username/password).
        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = Uri.UnescapeDataString(uri.UserInfo).Split(':', 2);
            webProxy.Credentials = new NetworkCredential(parts[0], parts.Length > 1 ? parts[1] : string.Empty);
        }

        // SOCKS5 поддерживается SocketsHttpHandler (.NET Core 3.0+); WinHTTP-обработчик его не понимает.
        return new SocketsHttpHandler
        {
            Proxy = webProxy,
            UseProxy = true,
        };
    }
}
