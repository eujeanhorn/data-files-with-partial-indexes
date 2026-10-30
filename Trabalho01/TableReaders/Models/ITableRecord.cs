namespace ECommerceDatabase.TableReaders.Models;

public interface ITableRecord<TKey> where TKey : IComparable<TKey>
{
    public TKey Key { get; }
    public byte Flags { get; }
    public long Continuation { get; }
}
