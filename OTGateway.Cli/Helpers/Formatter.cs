namespace OTGateway.Cli.Helpers;

/// <summary>
/// Утилиты вывода в консоль: цветной текст, таблицы, форматирование значений.
/// Методы вывода принимают необязательный <see cref="TextWriter"/> (по умолчанию <see cref="Console.Out"/>),
/// чтобы тесты могли перехватывать вывод напрямую, не подменяя консоль.
/// </summary>
public static class Formatter
{
    /// <summary>Печатает строку заданным цветом и восстанавливает предыдущий.</summary>
    public static void WriteColored(string text, ConsoleColor color, TextWriter? writer = null)
    {
        writer ??= Console.Out;
        var previous = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            writer.Write(text);
        }
        finally
        {
            Console.ForegroundColor = previous;
        }
    }

    public static void WriteLineColored(string text, ConsoleColor color, TextWriter? writer = null)
    {
        WriteColored(text, color, writer);
        (writer ??= Console.Out).WriteLine();
    }

    /// <summary>Логическое значение в виде ✔ / ✘.</summary>
    public static string Bool(bool? value) => value switch
    {
        true => "✔",
        false => "✘",
        null => "—",
    };

    /// <summary>Температура в формате «N1°C» (или «—» если нет данных).</summary>
    public static string Temp(double? value, string unit = "°C") =>
        value.HasValue ? $"{value.Value.ToString("N1")} {unit}" : "—";

    /// <summary>Число с точностью 1 знак или «—».</summary>
    public static string Num(double? value) =>
        value.HasValue ? value.Value.ToString("N1") : "—";

    /// <summary>
    /// Печатает таблицу с выровненными по ширине колонок строками.
    /// Пустые/нулевые заголовки допустимы.
    /// </summary>
    public static void Table(string[] headers, IReadOnlyList<string[]> rows, TextWriter? writer = null)
    {
        writer ??= Console.Out;

        if (rows.Count == 0)
        {
            writer.WriteLine("(нет данных)");
            return;
        }

        var columns = headers.Length;
        var widths = new int[columns];
        for (var c = 0; c < columns; c++)
        {
            widths[c] = headers[c].Length;
            foreach (var row in rows)
            {
                var cell = c < row.Length ? row[c] : string.Empty;
                widths[c] = Math.Max(widths[c], cell.Length);
            }
        }

        void PrintRow(string[] cells, bool header)
        {
            for (var c = 0; c < columns; c++)
            {
                var cell = c < cells.Length ? cells[c] : string.Empty;
                var padded = cell.PadRight(widths[c]);
                if (header)
                {
                    WriteColored(padded, ConsoleColor.Cyan, writer);
                }
                else
                {
                    writer.Write(padded);
                }

                if (c < columns - 1)
                {
                    writer.Write("  ");
                }
            }

            writer.WriteLine();
        }

        PrintRow(headers, header: true);
        writer.WriteLine(new string('─', widths.Sum() + 2 * (columns - 1)));
        foreach (var row in rows)
        {
            PrintRow(row, header: false);
        }
    }
}
