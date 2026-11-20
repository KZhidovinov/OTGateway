using System.Net;

namespace OTGateway.Cli.Services;

/// <summary>
/// Ошибка взаимодействия с HTTP API шлюза.
/// Хранит URL, HTTP-статус и тело ответа для дружелюбного вывода.
/// </summary>
public sealed class GatewayException : Exception
{
    public string? Url { get; }
    public HttpStatusCode? StatusCode { get; }
    public string? ResponseBody { get; }

    public GatewayException(string message)
        : base(message)
    {
    }

    public GatewayException(string message, string? url, HttpStatusCode? statusCode, string? responseBody)
        : base(message)
    {
        Url = url;
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public GatewayException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
