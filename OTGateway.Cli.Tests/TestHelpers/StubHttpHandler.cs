using System.Net;
using System.Text;

namespace OTGateway.Cli.Tests.TestHelpers;

/// <summary>
/// Заглушка HttpMessageHandler: возвращает заготовленные ответы, записывает запросы и их тела.
/// </summary>
public sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public List<HttpRequestMessage> Requests { get; } = new();
    public List<string?> RequestBodies { get; } = new();

    public StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    /// <summary>Всегда отвечает одним и тем же статусом/телом.</summary>
    public StubHttpHandler(HttpStatusCode status, string body = "", string contentType = "application/json")
        : this(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, contentType),
        })
    {
    }

    /// <summary>Всегда 200 с JSON-телом.</summary>
    public static StubHttpHandler Json(string body) => new(HttpStatusCode.OK, body);

    public int Count => Requests.Count;

    /// <summary>Отвечает по очереди набором (статус, тело); после исчерпания повторяет последний.</summary>
    public static StubHttpHandler Sequence(params (HttpStatusCode Status, string Body)[] responses)
    {
        var index = 0;
        return new StubHttpHandler(_ =>
        {
            var (status, body) = responses[Math.Min(index, responses.Length - 1)];
            index++;
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        });
    }

    /// <summary>Всегда отвечает одним и тем же статусом/телом.</summary>
    public static StubHttpHandler Repeat(HttpStatusCode status, string body) =>
        new(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
        return _responder(request);
    }

    public HttpRequestMessage LastRequest => Requests.Count > 0 ? Requests[^1] : throw new InvalidOperationException("Запросов не было");
    public string? LastBody => RequestBodies.Count > 0 ? RequestBodies[^1] : throw new InvalidOperationException("Запросов не было");
}
