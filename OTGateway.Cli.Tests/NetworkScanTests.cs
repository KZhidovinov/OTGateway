using System.Net;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using OTGateway.Cli.Services;
using OTGateway.Cli.Tests.TestHelpers;

namespace OTGateway.Cli.Tests;

/// <summary>
/// Тесты опроса сканирования Wi-Fi против РЕАЛЬНОго поведения прошивки:
/// пока поиск не завершён, устройство отдаёт 404; 200 приходит со списком сетей.
/// Тайминг детерминирован через фейковый <see cref="TimeProvider"/>.
/// </summary>
public class NetworkScanTests
{
    private static GatewayClient Create(StubHttpHandler handler, int scanBudgetSeconds) =>
        new("http://127.0.0.1", null, null, 30, NullLogger<GatewayClient>.Instance, handler, scanBudgetSeconds, new FakeTimeProvider());

    [Fact]
    public async Task GetNetworkScanAsync_KeepsPollingThrough404UntilArray()
    {
        // 404 (скан идёт) → 404 → 200 со списком.
        var handler = StubHttpHandler.Sequence(
            (HttpStatusCode.NotFound, ""),
            (HttpStatusCode.NotFound, ""),
            (HttpStatusCode.OK, """[{"ssid":"Home","signalQuality":80,"auth":3}]"""));

        using var client = Create(handler, scanBudgetSeconds: 60);

        var result = await client.GetNetworkScanAsync();

        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal("Home", result[0].GetProperty("ssid").GetString());
        Assert.Equal(3, handler.Count);
    }

    [Fact]
    public async Task GetNetworkScanAsync_Treats202AsInProgress()
    {
        var handler = StubHttpHandler.Sequence(
            (HttpStatusCode.Accepted, ""),
            (HttpStatusCode.OK, """[{"ssid":"X"}]"""));

        using var client = Create(handler, scanBudgetSeconds: 60);

        var result = await client.GetNetworkScanAsync();

        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(2, handler.Count);
    }

    [Fact]
    public async Task GetNetworkScanAsync_ThrowsOnUnauthorized()
    {
        var handler = StubHttpHandler.Sequence((HttpStatusCode.Unauthorized, "no"));

        using var client = Create(handler, scanBudgetSeconds: 60);

        await Assert.ThrowsAsync<GatewayException>(() => client.GetNetworkScanAsync());
    }

    [Fact]
    public async Task GetNetworkScanAsync_ThrowsWhenDeadlineExceeded()
    {
        // Прошивка никогда не завершает скан — бесконечные 404 до истечения бюджета.
        var handler = StubHttpHandler.Repeat(HttpStatusCode.NotFound, "");

        using var client = Create(handler, scanBudgetSeconds: 5);

        var ex = await Assert.ThrowsAsync<GatewayException>(() => client.GetNetworkScanAsync());
        Assert.Contains("5 с", ex.Message);
        Assert.True(handler.Count >= 3); //deadline срабатывает после нескольких опросов
    }

    /// <summary>Фейковый провайдер времени: мгновенные «паузы» и детерминированный ход времени.</summary>
    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UnixEpoch;

        public override DateTimeOffset GetUtcNow() => _now;

        // Создаём таймер, который тут же «срабатывает», сдвигая время на dueTime (интервал опроса).
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            _now += dueTime;
            callback(state);
            return new NoopTimer();
        }

        private sealed class NoopTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
