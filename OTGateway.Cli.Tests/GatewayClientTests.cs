using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using OTGateway.Cli.Services;
using OTGateway.Cli.Tests.TestHelpers;

namespace OTGateway.Cli.Tests;

public class GatewayClientTests
{
    private static GatewayClient Create(StubHttpHandler handler, string baseUrl = "http://127.0.0.1") =>
        new(baseUrl, null, null, 30, NullLogger<GatewayClient>.Instance, handler);

    [Fact]
    public async Task GetVarsAsync_RequestsApiVarsAndParsesJson()
    {
        var handler = StubHttpHandler.Json("""{"master":{"heating":{"targetTemp":60.5}}}""");
        using var client = Create(handler);

        var result = await client.GetVarsAsync();

        Assert.Equal("http://127.0.0.1/api/vars", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("GET", handler.LastRequest.Method.Method);
        Assert.Equal(60.5, result.GetProperty("master").GetProperty("heating").GetProperty("targetTemp").GetDouble());
    }

    [Fact]
    public async Task GetSettingsAsync_NonSuccess_ThrowsGatewayExceptionWithDetails()
    {
        var handler = new StubHttpHandler(HttpStatusCode.InternalServerError, "oops");
        using var client = Create(handler);

        var ex = await Assert.ThrowsAsync<GatewayException>(() => client.GetSettingsAsync());

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Equal("oops", ex.ResponseBody);
        Assert.Contains("500", ex.Message);
    }

    [Fact]
    public async Task GetVersionAsync_RequestsApiInfo()
    {
        var handler = StubHttpHandler.Json("""{"build":{"version":"1.2.3"},"chip":{"model":"ESP8266"}}""");
        using var client = Create(handler);

        var result = await client.GetVersionAsync();

        Assert.Equal("http://127.0.0.1/api/info", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("GET", handler.LastRequest.Method.Method);
        Assert.Equal("1.2.3", result.GetProperty("build").GetProperty("version").GetString());
    }

    [Fact]
    public async Task GetVersionAsync_InvalidJson_ThrowsGatewayException()
    {
        var handler = StubHttpHandler.Json("<html>not json</html>");
        using var client = Create(handler);

        await Assert.ThrowsAsync<GatewayException>(() => client.GetVersionAsync());
    }

    [Fact]
    public async Task PostSettingsAsync_SendsRawPatchAsJson()
    {
        var handler = StubHttpHandler.Json("");
        using var client = Create(handler);
        using var patch = JsonDocument.Parse("""{"heating":{"target":55}}""");

        await client.PostSettingsAsync(patch.RootElement.Clone());

        Assert.Equal("POST", handler.LastRequest.Method.Method);
        Assert.Equal("http://127.0.0.1/api/settings", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("""{"heating":{"target":55}}""", handler.LastBody);
        Assert.Equal("application/json", handler.LastRequest.Content!.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task PostVarsAsync_SerializesActionsPayload()
    {
        var handler = StubHttpHandler.Json("");
        using var client = Create(handler);

        await client.PostVarsAsync(new { actions = new { resetFault = true } });

        Assert.Equal("""{"actions":{"resetFault":true}}""", handler.LastBody);
    }

    [Fact]
    public async Task GetSensorsAsync_Detailed_AppendsQueryParam()
    {
        var handler = StubHttpHandler.Json("{}");
        using var client = Create(handler);

        await client.GetSensorsAsync(detailed: true);

        Assert.Equal("http://127.0.0.1/api/sensors?detailed=1", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task PostSensorAsync_EscapesIdInQuery()
    {
        var handler = StubHttpHandler.Json("");
        using var client = Create(handler);
        using var payload = JsonDocument.Parse("""{"enabled":true}""");

        await client.PostSensorAsync("a b&c", payload.RootElement.Clone());

        Assert.Contains("id=a%20b%26c", handler.LastRequest.RequestUri!.Query);
        Assert.Equal("""{"enabled":true}""", handler.LastBody);
    }

    [Fact]
    public async Task GetNetworkScanAsync_ReturnsArrayOnImmediate200()
    {
        var handler = StubHttpHandler.Json("""[{"ssid":"Home","signalQuality":80,"auth":3}]""");
        using var client = Create(handler);

        var result = await client.GetNetworkScanAsync();

        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal("Home", result[0].GetProperty("ssid").GetString());
    }

    [Fact]
    public async Task DownloadBackupAsync_ReturnsRawBytes()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, "backup-payload", "application/octet-stream");
        using var client = Create(handler);

        var bytes = await client.DownloadBackupAsync();

        Assert.Equal(Encoding.UTF8.GetBytes("backup-payload"), bytes);
    }

    [Fact]
    public async Task UploadBackupAsync_PostsJsonContentType()
    {
        var handler = StubHttpHandler.Json("");
        using var client = Create(handler);

        await client.UploadBackupAsync("""{"heating":{}}"""u8.ToArray());

        Assert.Equal("http://127.0.0.1/api/backup/restore", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("application/json", handler.LastRequest.Content!.Headers.ContentType!.MediaType);
        Assert.Equal("""{"heating":{}}""", handler.LastBody);
    }

    [Fact]
    public async Task RestartAsync_PostsRestartActionToVars()
    {
        var handler = StubHttpHandler.Json("");
        using var client = Create(handler);

        await client.RestartAsync();

        Assert.Equal("POST", handler.LastRequest.Method.Method);
        Assert.Equal("http://127.0.0.1/api/vars", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("""{"actions":{"restart":true}}""", handler.LastBody);
    }

    [Fact]
    public async Task RestartAsync_ToleratesServiceUnavailable()
    {
        // 503 = vars.states.restarting — устройство уже в ребуте.
        var handler = new StubHttpHandler(HttpStatusCode.ServiceUnavailable);
        using var client = Create(handler);

        await client.RestartAsync(); // не должно бросать
    }

    [Fact]
    public async Task RestartAsync_ToleratesConnectionDrop()
    {
        // Соединение разорвано, когда устройство перезагружается.
        var handler = new StubHttpHandler(_ => throw new HttpRequestException("connection refused"));
        using var client = Create(handler);

        await client.RestartAsync(); // не должно бросать
    }

    [Fact]
    public async Task RestartAsync_ThrowsOnRealError()
    {
        var handler = new StubHttpHandler(HttpStatusCode.Unauthorized, "no");
        using var client = Create(handler);

        await Assert.ThrowsAsync<GatewayException>(() => client.RestartAsync());
    }

    [Fact]
    public async Task UnreachableHost_ThrowsGatewayException()
    {
        var handler = new StubHttpHandler(_ => throw new HttpRequestException("connection refused"));
        using var client = Create(handler);

        var ex = await Assert.ThrowsAsync<GatewayException>(() => client.GetVarsAsync());
        Assert.Contains("127.0.0.1", ex.Message);
    }

    [Fact]
    public void BaseUrl_TrimsTrailingSlash()
    {
        var handler = StubHttpHandler.Json("{}");
        using var client = Create(handler, "http://gateway.local:8080/");

        Assert.Equal("http://gateway.local:8080", client.BaseUrl);
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("", "", null)]
    [InlineData("admin", "s3cret", "Basic YWRtaW46czNjcmV0")]
    public void BuildAuthorizationHeader(string? user, string? pass, string? expected)
    {
        var header = GatewayClient.BuildAuthorizationHeader(user, pass);

        if (expected is null)
        {
            Assert.Null(header);
            return;
        }

        Assert.NotNull(header);
        Assert.Equal("Basic", header.Scheme);
        Assert.Equal($"{header.Scheme} {header.Parameter}", expected);
    }

    [Fact]
    public void BuildAuthorizationHeader_Base64IsUtf8()
    {
        var header = GatewayClient.BuildAuthorizationHeader("юзер", "päss");

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header!.Parameter!));
        Assert.Equal("юзер:päss", decoded);
    }

    [Fact]
    public async Task Credentials_SetAuthorizationHeaderOnRequests()
    {
        var handler = StubHttpHandler.Json("{}");
        using var client = new GatewayClient("http://127.0.0.1", "admin", "pass", 30, NullLogger<GatewayClient>.Instance, handler);

        await client.GetVarsAsync();

        var expected = new AuthenticationHeaderValue("Basic", Convert.ToBase64String("admin:pass"u8));
        Assert.Equal(expected, handler.LastRequest.Headers.Authorization);
    }
}
