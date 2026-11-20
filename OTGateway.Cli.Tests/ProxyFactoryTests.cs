using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using OTGateway.Cli.Services;

namespace OTGateway.Cli.Tests;

public class ProxyFactoryTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("socks5://127.0.0.1:1080", true)]
    [InlineData("http://proxy.local:3128", true)]
    [InlineData("https://proxy.local:3129", true)]
    [InlineData("ftp://host:21", false)]
    [InlineData("127.0.0.1:1080", false)]
    [InlineData("socks5:/bad url", false)]
    public void TryValidate_ChecksScheme(string? proxy, bool expected) =>
        Assert.Equal(expected, ProxyFactory.TryValidate(proxy, out _));

    [Fact]
    public void TryValidate_Invalid_FillsError()
    {
        Assert.False(ProxyFactory.TryValidate("ftp://host", out var error));
        Assert.Contains("--proxy", error);
    }

    [Fact]
    public void CreateHandler_NoProxy_ReturnsPlainHttpClientHandler()
    {
        using var handler = ProxyFactory.CreateHandler(null);
        Assert.IsType<HttpClientHandler>(handler);
    }

    [Fact]
    public void CreateHandler_Socks5_ReturnsSocketsHandlerWithWebProxy()
    {
        using var handler = ProxyFactory.CreateHandler("socks5://192.168.1.9:1080");

        var sockets = Assert.IsType<SocketsHttpHandler>(handler);
        Assert.True(sockets.UseProxy);
        var web = Assert.IsType<WebProxy>(sockets.Proxy);
        Assert.Equal(new Uri("socks5://192.168.1.9:1080"), web.Address);
    }

    [Fact]
    public void CreateHandler_Socks5WithUserInfo_SetsNetworkCredential()
    {
        using var handler = ProxyFactory.CreateHandler("socks5://bob:p%40ss@10.0.0.1:1080");

        var sockets = Assert.IsType<SocketsHttpHandler>(handler);
        var web = Assert.IsType<WebProxy>(sockets.Proxy);
        var credential = Assert.IsType<NetworkCredential>(web.Credentials);
        Assert.Equal("bob", credential.UserName);
        Assert.Equal("p@ss", credential.Password);
    }

    [Fact]
    public void GatewayClient_WithProxy_ConstructsSuccessfully()
    {
        // Конструкция не выполняет сеть — проверяем только, что прокси принимается.
        using var client = new GatewayClient(
            "http://192.168.1.50", null, null, 30, NullLogger<GatewayClient>.Instance,
            proxy: "socks5://127.0.0.1:1080");

        Assert.Equal("http://192.168.1.50", client.BaseUrl);
    }

    [Fact]
    public void UpgradeClient_WithProxy_ConstructsSuccessfully()
    {
        using var handler = ProxyFactory.CreateHandler("socks5://127.0.0.1:1080");
        using var client = new UpgradeClient(
            "http://192.168.1.50", null, null, NullLogger<UpgradeClient>.Instance, handler);

        Assert.NotNull(client);
    }
}
