using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using OTGateway.Cli.Services;
using OTGateway.Cli.Tests.TestHelpers;

namespace OTGateway.Cli.Tests;

public class UpgradeClientTests
{
    private static UpgradeClient Create(StubHttpHandler handler) =>
        new("http://127.0.0.1", null, null, NullLogger<UpgradeClient>.Instance, handler);

    [Fact]
    public async Task UpgradeAsync_WithoutStreams_ThrowsArgumentException()
    {
        var handler = StubHttpHandler.Json("{}");
        using var client = Create(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => client.UpgradeAsync(null, null, null, null));
    }

    [Fact]
    public async Task UpgradeAsync_SendsMultipartWithFirmwareField()
    {
        var handler = StubHttpHandler.Json("""{"firmware":{"status":2,"error":""}}""");
        using var client = Create(handler);
        using var firmware = new MemoryStream([1, 2, 3, 4]);

        var result = await client.UpgradeAsync(firmware, "fw.bin", null, null);

        Assert.Equal("POST", handler.LastRequest.Method.Method);
        Assert.Equal("http://127.0.0.1/api/upgrade", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("multipart/form-data", handler.LastRequest.Content!.Headers.ContentType!.MediaType);
        Assert.Contains("name=firmware", handler.LastBody);
        Assert.Contains("filename=fw.bin", handler.LastBody);
        Assert.DoesNotContain("name=filesystem", handler.LastBody);

        Assert.True(result.Success);
        Assert.Equal(2, result.Firmware!.Status);
        Assert.Equal("OK", result.Firmware.StatusText);
        Assert.Null(result.Filesystem);
    }

    [Fact]
    public async Task UpgradeAsync_BothStreams_SendsBothFieldsWithDefaults()
    {
        var handler = StubHttpHandler.Json("""{"firmware":{"status":4,"error":"aborted by user"},"filesystem":{"status":2}}""");
        using var client = Create(handler);
        using var fw = new MemoryStream([9]);
        using var fs = new MemoryStream([8]);

        var result = await client.UpgradeAsync(fw, null, fs, null);

        Assert.Contains("name=firmware", handler.LastBody);
        Assert.Contains("filename=firmware.bin", handler.LastBody);
        Assert.Contains("name=filesystem", handler.LastBody);
        Assert.Contains("filename=filesystem.bin", handler.LastBody);

        Assert.False(result.Success);
        Assert.Equal("Aborted", result.Firmware!.StatusText);
        Assert.Equal("firmware: Aborted (aborted by user), filesystem: OK", result.Describe());
    }

    [Fact]
    public async Task UpgradeAsync_EmptyBody_ReturnsResultWithoutComponents()
    {
        var handler = StubHttpHandler.Json("");
        using var client = Create(handler);
        using var fw = new MemoryStream([1]);

        var result = await client.UpgradeAsync(fw, "fw.bin", null, null);

        Assert.True(result.Success);
        Assert.Null(result.Firmware);
        Assert.Equal("нет данных", result.Describe());
    }

    [Fact]
    public async Task UpgradeAsync_NonSuccessStatus_ThrowsGatewayException()
    {
        var handler = new StubHttpHandler(HttpStatusCode.BadRequest, "bad request");
        using var client = Create(handler);
        using var fw = new MemoryStream([1]);

        var ex = await Assert.ThrowsAsync<GatewayException>(() => client.UpgradeAsync(fw, "fw.bin", null, null));
        Assert.Contains("400", ex.Message);
    }

    [Fact]
    public async Task UpgradeAsync_InvalidJson_ThrowsGatewayException()
    {
        var handler = StubHttpHandler.Json("}{");
        using var client = Create(handler);
        using var fw = new MemoryStream([1]);

        await Assert.ThrowsAsync<GatewayException>(() => client.UpgradeAsync(fw, "fw.bin", null, null));
    }
}
