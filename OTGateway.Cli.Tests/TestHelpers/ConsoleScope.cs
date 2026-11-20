using System.Threading;
using Xunit;

namespace OTGateway.Cli.Tests.TestHelpers;

/// <summary>
/// Перехват вывода Console.Out. Все консольные тесты должны идти в коллекции "Console",
/// чтобы не мешать друг другу (Capture()/Dispose() под общим замком).
/// </summary>
public sealed class ConsoleScope : IDisposable
{
    internal static readonly object Sync = new();
    private readonly TextWriter _original;
    private readonly StringWriter _captured = new();

    private ConsoleScope(TextWriter original) => _original = original;

    public string Output => _captured.ToString();

    /// <summary>Захватывает консоль. Обязан быть вызван внутри [Collection("Console")].</summary>
    public static ConsoleScope Capture()
    {
        Monitor.Enter(Sync);
        var scope = new ConsoleScope(Console.Out);
        Console.SetOut(scope._captured);
        return scope;
    }

    public void Dispose()
    {
        Console.SetOut(_original);
        Monitor.Exit(Sync);
    }
}

/// <summary>Коллекция, отключающая параллельное выполнение для консольных тестов.</summary>
[CollectionDefinition("Console", DisableParallelization = true)]
public sealed class ConsoleCollectionDefinition;
