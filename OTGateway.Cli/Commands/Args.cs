namespace OTGateway.Cli.Commands;

/// <summary>
/// Минимальный ручной разбор аргументов команды: позиционные значения и флаги --name value / --flag.
/// </summary>
public sealed class Args
{
    private readonly List<string> _positional = new();
    private readonly Dictionary<string, string?> _flags = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> Positional => _positional;

    public static Args Parse(string[] args)
    {
        var result = new Args();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                var name = arg[2..];
                var eq = name.IndexOf('=');
                if (eq >= 0)
                {
                    result._flags[name[..eq]] = name[(eq + 1)..];
                }
                else if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    // Флаг со значением (например --watch 5). Значение берём, если оно не выглядит как флаг.
                    result._flags[name] = args[++i];
                }
                else
                {
                    result._flags[name] = null; // булев флаг
                }
            }
            else
            {
                result._positional.Add(arg);
            }
        }

        return result;
    }

    public bool HasFlag(string name) => _flags.ContainsKey(name);

    /// <summary>Настоящий булев флаг (--flag без значения) либо значение "true".</summary>
    public bool IsSet(string name) =>
        _flags.TryGetValue(name, out var v) && (v is null || v.Equals("true", StringComparison.OrdinalIgnoreCase));

    public string? Get(string name) => _flags.TryGetValue(name, out var v) ? v : null;

    public string Get(string name, string fallback) => _flags.TryGetValue(name, out var v) && v is not null ? v : fallback;

    public int GetInt(string name, int fallback) =>
        _flags.TryGetValue(name, out var v) && int.TryParse(v, out var n) ? n : fallback;

    public string? this[int index] => index < _positional.Count ? _positional[index] : null;
}
