using System.Text;
using OTGateway.Cli;
using OTGateway.Cli.Commands;

namespace OTGateway.Cli.Tests;

public class BackupRestoreValidationTests
{
    private static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void ValidateRestorePayload_ValidJson_ReturnsNull()
    {
        Assert.Null(BackupCommand.ValidateRestorePayload(Bytes("""{"heating":{"target":55}}""")));
    }

    [Fact]
    public void ValidateRestorePayload_TooSmall_Fails()
    {
        var error = BackupCommand.ValidateRestorePayload(Bytes("[]"));

        Assert.NotNull(error);
        Assert.Contains("маленький", error);
    }

    [Fact]
    public void ValidateRestorePayload_ExceedsDeviceLimit_FailsWithLimit()
    {
        // Больше лимита устройства (2536 байт) → локальная ошибка до POST.
        var big = Bytes("{" + new string(' ', BackupCommand.MaxRestoreBytes + 10) + "}");

        var error = BackupCommand.ValidateRestorePayload(big);

        Assert.NotNull(error);
        Assert.Contains(BackupCommand.MaxRestoreBytes.ToString(), error);
    }

    [Fact]
    public void ValidateRestorePayload_InvalidJson_Fails()
    {
        var error = BackupCommand.ValidateRestorePayload(Bytes("{oops"));

        Assert.NotNull(error);
        Assert.Contains("JSON", error);
    }

    [Fact]
    public void ValidateRestorePayload_Null_Fails() =>
        Assert.NotNull(BackupCommand.ValidateRestorePayload(null!));
}

public class AuthResolutionTests
{
    private static Func<string, string?> Env(Dictionary<string, string> values) =>
        key => values.TryGetValue(key, out var v) ? v : null;

    [Fact]
    public void CliFlags_WinOverEnvironment()
    {
        var env = Env(new() { ["OTG_ADDRESS"] = "env-host", ["OTG_USER"] = "envuser", ["OTG_PASSWORD"] = "envpass" });

        var (address, user, password) = Program.ResolveAuthOptions("cli-host", "cliuser", "clipass", env);

        Assert.Equal("cli-host", address);
        Assert.Equal("cliuser", user);
        Assert.Equal("clipass", password);
    }

    [Fact]
    public void Address_FallsBackToEnvironment()
    {
        var env = Env(new() { ["OTG_ADDRESS"] = "192.168.1.50" });

        var (address, _, _) = Program.ResolveAuthOptions(null, null, null, env);

        Assert.Equal("192.168.1.50", address);
    }

    [Fact]
    public void UserAndPassword_FromSeparateEnvVars()
    {
        var env = Env(new() { ["OTG_USER"] = "admin", ["OTG_PASSWORD"] = "s3cret", ["OTG_TOKEN"] = "ignored:me" });

        var (_, user, password) = Program.ResolveAuthOptions("h", null, null, env);

        // OTG_USER/OTG_PASSWORD имеют приоритет над OTG_TOKEN.
        Assert.Equal("admin", user);
        Assert.Equal("s3cret", password);
    }

    [Fact]
    public void Token_UsedWhenNoUserOrPassword()
    {
        var env = Env(new() { ["OTG_TOKEN"] = "login:pass" });

        var (_, user, password) = Program.ResolveAuthOptions("h", null, null, env);

        Assert.Equal("login", user);
        Assert.Equal("pass", password);
    }

    [Fact]
    public void Environment_FillsMissingCredentialField_WhenOnlyOneFlagGiven()
    {
        // CLI задал только --user, пароль берём из OTG_PASSWORD (флаг главнее для user).
        var env = Env(new() { ["OTG_USER"] = "envuser", ["OTG_PASSWORD"] = "envpass" });

        var (_, user, password) = Program.ResolveAuthOptions("h", "cliuser", null, env);

        Assert.Equal("cliuser", user);
        Assert.Equal("envpass", password);
    }

    [Fact]
    public void NoEnvAndNoFlags_StayNull()
    {
        var (_, user, password) = Program.ResolveAuthOptions("h", null, null, _ => null);

        Assert.Null(user);
        Assert.Null(password);
    }
}
