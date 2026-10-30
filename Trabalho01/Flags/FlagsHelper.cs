using ECommerceDatabase.TableReaders.Models;

namespace ECommerceDatabase.Flags;

public static class FlagsHelper
{
    private const int DeletedBit = 0;
    private const int ExtensionBit = 1;

    public static bool RecordIsDeleted<TKey>(ITableRecord<TKey> record) where TKey : IComparable<TKey>
        => RecordIsDeleted(record.Flags);

    public static bool RecordIsDeleted(byte flags)
        => GetBit(flags, DeletedBit);

    public static byte SetAsDeleted(byte flags)
        => SetBit(flags, DeletedBit, true);

    // Registros de extensão são os inseridos no final do arquivo, fora da área ordenada.
    public static bool RecordIsExtension<TKey>(ITableRecord<TKey> record) where TKey : IComparable<TKey>
        => GetBit(record.Flags, ExtensionBit);

    public static byte SetAsExtension(byte flags)
        => SetBit(flags, ExtensionBit, true);

    private static bool GetBit(byte value, int index)
        => (value & (1 << index)) != 0;

    private static byte SetBit(byte value, int index, bool state)
    {
        var mask = (byte)(1 << index);

        return state
            ? (byte)(value | mask)
            : (byte)(value & ~mask);
    }
}
