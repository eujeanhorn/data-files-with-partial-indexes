using ECommerceDatabase.Consoles;
using ECommerceDatabase.TableReaders.Models;
using ECommerceDatabase.TeableViewers.Columns;
using System.Text;

namespace ECommerceDatabase.TeableViewers;

internal class TableViewer<TRecord>
{
    private readonly List<TableViewerColumn<TRecord>> _columns;

    public TableViewer()
    {
        _columns = [];
    }

    public TableViewer<TRecord> AddColumn(string name, Func<TRecord, string> valueGetter, int length)
    {
        _columns.Add(new(name, valueGetter, length));
        return this;
    }

    public void Run(IEnumerable<TablePage<TRecord>> pages, int maxPageSize)
    {
        var header = BuildHeader();
        var lineSeparator = BuildLineSeparator();

        foreach (var page in pages)
        {
            Console.Clear();

            Console.Write(header);
            foreach (var record in page.Records)
                Console.Write(BuildRecordLine(record));

            Console.Write(lineSeparator);

            if (page.Records.Count < maxPageSize)
            {
                Console.WriteLine("=== FIM DOS RESULTADOS ===");
                Console.WriteLine("Pressione qualquer tecla para sair.");
                ConsoleUtils.WaitForAnyKey();
                break;
            }

            Console.WriteLine("...");
            Console.WriteLine("[Enter] Próxima página [Q] Sair");
            var pressedKey = ConsoleUtils.WaitForKeys(ConsoleKey.Q, ConsoleKey.Enter);
            if (pressedKey == ConsoleKey.Q)
                break;
        }
    }

    private string BuildHeader()
    {
        var stringBuilder = new StringBuilder();

        AddLineSeparator(stringBuilder, '=', '+');
        AddHeaderTitles(stringBuilder);
        AddLineSeparator(stringBuilder, '=', '+');

        return stringBuilder.ToString();
    }

    private string BuildLineSeparator(char line = '-', char cross = '+')
    {
        StringBuilder stringBuilder = new();
        AddLineSeparator(stringBuilder, line, cross);
        return stringBuilder.ToString();
    }

    private void AddLineSeparator(StringBuilder stringBuilder, char line = '-', char cross = '+')
    {
        stringBuilder.Append(cross);

        for (int i = 0; i < _columns.Count; i++)
        {
            stringBuilder.Append(line, _columns[i].Length + 2);
            stringBuilder.Append(cross);
        }

        stringBuilder.Append('\n');
    }

    private void AddHeaderTitles(StringBuilder stringBuilder, char separator = '|')
    {
        stringBuilder.Append(separator);
        stringBuilder.Append(' ');

        for (int i = 0; i < _columns.Count; i++)
        {
            stringBuilder.Append(NormalizeStringSize(_columns[i].Name, _columns[i].Length));

            if (i < _columns.Count - 1)
                stringBuilder.AppendFormat(" {0} ", separator);
        }

        stringBuilder.AppendFormat(" {0}\n", separator);
    }

    private string BuildRecordLine(TRecord record, char separator = '|')
    {
        StringBuilder stringBuilder = new();

        stringBuilder.Append(separator);
        stringBuilder.Append(' ');

        for (int i = 0; i < _columns.Count; i++)
        {
            var columnValue = _columns[i].ColumnValueGetter(record);
            stringBuilder.Append(NormalizeStringSize(columnValue, _columns[i].Length));

            if (i < _columns.Count - 1)
                stringBuilder.AppendFormat(" {0} ", separator);
        }

        stringBuilder.AppendFormat(" {0}\n", separator);

        return stringBuilder.ToString();
    }

    private static string NormalizeStringSize(string value, int length)
    {
        if (value.Length == length)
            return value;

        if (value.Length < length)
            return value + new string(' ', length - value.Length);

        return value[..length];
    }
}
