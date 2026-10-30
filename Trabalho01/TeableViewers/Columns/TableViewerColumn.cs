namespace ECommerceDatabase.TeableViewers.Columns;

internal class TableViewerColumn<TRecord>
{
    public string Name { get; }
    public Func<TRecord, string> ColumnValueGetter { get; }
    public int Length { get; }

    public TableViewerColumn(string name, Func<TRecord, string> columnValueGetter, int length)
    {
        Name = name;
        ColumnValueGetter = columnValueGetter;
        Length = length;
    }
}
