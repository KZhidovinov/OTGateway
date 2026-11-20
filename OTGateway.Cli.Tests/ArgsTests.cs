using OTGateway.Cli.Commands;

namespace OTGateway.Cli.Tests;

public class ArgsTests
{
    [Fact]
    public void Parse_PositionalArguments()
    {
        var args = Args.Parse(["set", "heating.target", "55"]);

        Assert.Equal("set", args[0]);
        Assert.Equal("heating.target", args[1]);
        Assert.Equal("55", args[2]);
        Assert.Null(args[3]);
    }

    [Fact]
    public void Parse_FlagWithValue()
    {
        var args = Args.Parse(["--watch", "5"]);

        Assert.True(args.HasFlag("watch"));
        Assert.Equal("5", args.Get("watch"));
        Assert.Empty(args.Positional);
    }

    [Fact]
    public void Parse_FlagEqualsSyntax()
    {
        var args = Args.Parse(["--watch=10"]);

        Assert.Equal("10", args.Get("watch"));
    }

    [Fact]
    public void Parse_BooleanFlagWithoutValue()
    {
        var args = Args.Parse(["--json", "--yes"]);

        Assert.True(args.IsSet("json"));
        Assert.True(args.IsSet("yes"));
        Assert.Null(args.Get("json"));
    }

    [Fact]
    public void Parse_FlagFollowedByFlagIsNull()
    {
        var args = Args.Parse(["--json", "--watch", "5"]);

        Assert.True(args.IsSet("json"));
        Assert.Equal("5", args.Get("watch"));
    }

    [Fact]
    public void Parse_MixedPositionalAndFlags()
    {
        var args = Args.Parse(["restore", "backup.json", "--yes"]);

        Assert.Equal("restore", args[0]);
        Assert.Equal("backup.json", args[1]);
        Assert.True(args.IsSet("yes"));
    }

    [Fact]
    public void Parse_FlagNamesAreCaseInsensitive()
    {
        var args = Args.Parse(["--JSON"]);

        Assert.True(args.IsSet("json"));
    }

    [Fact]
    public void IsSet_ExplicitTrueValue_Counts()
    {
        var args = Args.Parse(["--flag", "true"]);
        Assert.True(args.IsSet("flag"));

        var off = Args.Parse(["--flag", "false"]);
        Assert.False(off.IsSet("flag"));
    }

    [Fact]
    public void Get_Fallbacks()
    {
        var args = Args.Parse([]);

        Assert.Equal("default", args.Get("missing", "default"));
        Assert.Equal(42, args.GetInt("missing", 42));
        Assert.Null(args.Get("missing"));
    }

    [Fact]
    public void GetInt_ParsesValue()
    {
        var args = Args.Parse(["--timeout", "15"]);

        Assert.Equal(15, args.GetInt("timeout", 30));
    }

    [Fact]
    public void GetInt_FallsBackOnGarbage()
    {
        var args = Args.Parse(["--timeout", "abc"]);

        Assert.Equal(30, args.GetInt("timeout", 30));
    }
}
