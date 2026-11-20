using System.Text;
using System.Text.Json;

namespace OTGateway.Cli.Helpers;

/// <summary>
/// Работа с JSON: рекурсивный цветной вывод дерева, чтение из файла/stdin.
/// </summary>
public static class JsonExtensions
{
    /// <summary>Единый набор опций сериализации (см. <see cref="JsonConfig"/>).</summary>
    public static readonly JsonSerializerOptions Options = JsonConfig.Writing;

    /// <summary>Маска для секретных строк в не-JSON выводе дерева.</summary>
    private const string RedactedValue = "••••";

    /// <summary>
    /// Печатает JSON-дерево с подсветкой: ключи — cyan, строки — green, числа — yellow, bool — magenta.
    /// При <paramref name="redactSecrets"/> строковые значения листьев, чей ключ содержит «password»
    /// (без учёта регистра), заменяются на <c>••••</c>.
    /// </summary>
    public static void PrintJsonTree(JsonElement element, bool redactSecrets = false, TextWriter? writer = null) =>
        WriteValue(element, indent: 0, isLast: true, redactSecrets, writer ??= Console.Out, null);

    private static IEnumerable<(T Item, bool IsLast)> WithLast<T>(IEnumerable<T> source)
    {
        using var e = source.GetEnumerator();
        if (!e.MoveNext())
        {
            yield break;
        }

        var current = e.Current;
        while (e.MoveNext())
        {
            yield return (current, false);
            current = e.Current;
        }

        yield return (current, true);
    }

    /// <summary>Значение ключаleaf следует замаскировать, если имя содержит «password» и это строка.</summary>
    private static bool IsSecretKey(string? key) =>
        key is not null && key.Contains("password", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Печатает одно значение JSON. Открывающая скобка ставится на текущей строке (после «ключ: »),
    /// закрывающая — с отступом и завершающей запятой, если это не последний элемент.
    /// </summary>
    private static void WriteValue(
        JsonElement element,
        int indent,
        bool isLast,
        bool redactSecrets,
        TextWriter writer,
        string? keyName)
    {
        var suffix = isLast ? string.Empty : ",";
        var pad = new string(' ', indent * 2);
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (!element.EnumerateObject().Any())
                {
                    writer.WriteLine("{}" + suffix);
                    return;
                }

                writer.WriteLine("{");
                foreach (var (property, last) in WithLast(element.EnumerateObject()))
                {
                    WriteColored(new string(' ', (indent + 1) * 2) + $"\"{property.Name}\": ", ConsoleColor.Cyan, writer);

                    if (redactSecrets && IsSecretKey(property.Name) && property.Value.ValueKind == JsonValueKind.String)
                    {
                        Formatter.WriteColored($"\"{RedactedValue}\"", ConsoleColor.Green, writer);
                        writer.WriteLine(last ? string.Empty : ",");
                    }
                    else
                    {
                        WriteValue(property.Value, indent + 1, last, redactSecrets, writer, property.Name);
                    }
                }

                writer.WriteLine(pad + "}" + suffix);
                break;

            case JsonValueKind.Array:
                if (!element.EnumerateArray().Any())
                {
                    writer.WriteLine("[]" + suffix);
                    return;
                }

                writer.WriteLine("[");
                foreach (var (item, last) in WithLast(element.EnumerateArray()))
                {
                    WriteValue(item, indent + 1, last, redactSecrets, writer, null);
                }

                writer.WriteLine(pad + "]" + suffix);
                break;

            case JsonValueKind.String:
                Formatter.WriteColored($"\"{element.GetString()}\"", ConsoleColor.Green, writer);
                writer.WriteLine(suffix);
                break;

            case JsonValueKind.Number:
                Formatter.WriteColored(element.GetRawText(), ConsoleColor.Yellow, writer);
                writer.WriteLine(suffix);
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                Formatter.WriteColored(element.GetRawText(), ConsoleColor.Magenta, writer);
                writer.WriteLine(suffix);
                break;

            case JsonValueKind.Null:
                Formatter.WriteColored("null", ConsoleColor.DarkGray, writer);
                writer.WriteLine(suffix);
                break;

            default:
                writer.WriteLine(element.GetRawText() + suffix);
                break;
        }
    }

    public static void WriteColored(string text, ConsoleColor color, TextWriter? writer = null) =>
        Formatter.WriteColored(text, color, writer);

    /// <summary>
    /// Читает JSON из файла по пути, либо из stdin, если путь пуст или равен "-".
    /// </summary>
    public static JsonElement ParseJsonFromFileOrStdin(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "-")
        {
            var text = Console.In.ReadToEnd();
            return ParseOrThrow(text, "stdin");
        }

        return ReadJsonFile(path);
    }

    /// <summary>Читает и парсит JSON-файл.</summary>
    public static JsonElement ReadJsonFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Файл не найден: {path}", path);
        }

        var text = File.ReadAllText(path, Encoding.UTF8);
        return ParseOrThrow(text, path);
    }

    private static JsonElement ParseOrThrow(string text, string source)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidDataException($"Пустой JSON в источнике: {source}");
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            return doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Некорректный JSON в {source}: {ex.Message}", ex);
        }
    }
}
