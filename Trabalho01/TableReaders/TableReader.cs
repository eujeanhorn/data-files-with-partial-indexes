using ECommerceDatabase.Flags;
using ECommerceDatabase.TableReaders.Models;
using System.Buffers.Binary;
using System.Text;

namespace ECommerceDatabase.TableReaders;

public abstract class TableReader<TRecord, TKey> : IDisposable where TRecord : class, ITableRecord<TKey> where TKey : IComparable<TKey>
{
    public const long EndOfTableContinuation = long.MaxValue;

    private const int IndexHeaderSize = sizeof(long) + sizeof(int);

    private readonly byte[] _sharedBuffer;
    private readonly byte[] _writeBuffer;
    private readonly FileStream _fileStream;
    private readonly string _indexFilePath;
    private List<IndexEntry>? _index;
    private long _indexMainRecordsCount;
    private bool Disposed { get; set; }

    private int RecordSize => _sharedBuffer.Length;
    private int ContinuationOffset => RecordSize - 1 - sizeof(long);
    private int FlagsOffset => ContinuationOffset - 1;

    // A chave deve ocupar os primeiros KeySize bytes do registro.
    protected abstract int KeySize { get; }
    // Quantidade de registros da área principal entre duas entradas do índice parcial.
    protected abstract int IndexInterval { get; }

    protected TableReader(byte[] sharedBuffer, string filePath)
    {
        _sharedBuffer = sharedBuffer;
        _writeBuffer = new byte[sharedBuffer.Length];
        _fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        _indexFilePath = Path.ChangeExtension(filePath, ".idx");
        Disposed = false;
    }

    public IEnumerable<TRecord> GetAll()
        => EnumerateFrom(0, GetMainRecordsCount())
            .Select(r => r.Record)
            .Where(r => !FlagsHelper.RecordIsDeleted(r));

    public IEnumerable<TablePage<TRecord>> GetAllPaginated(int countPerPage)
    {
        List<TRecord> currentPage = new(capacity: countPerPage);

        foreach (var record in GetAll())
        {
            currentPage.Add(record);

            if (currentPage.Count < countPerPage)
                continue;

            yield return new(currentPage);
            currentPage = new(capacity: countPerPage);
        }

        if (currentPage.Count > 0)
            yield return new(currentPage);
    }

    public TRecord? TryGetRecord(TKey key)
        => TryFindRecord(key)?.Record;

    public TRecord? TryGetRecordByIndex(TKey key)
    {
        var mainRecordsCount = GetMainRecordsCount();
        var index = LoadIndex(mainRecordsCount);

        // Última entrada com chave menor que a pesquisada: o registro, se existir, está a partir dela.
        int first = 0, last = index.Count;
        while (first < last)
        {
            var middle = first + (last - first) / 2;
            if (index[middle].Key.CompareTo(key) < 0)
                first = middle + 1;
            else
                last = middle;
        }

        var startPosition = first > 0 ? index[first - 1].Position : 0;
        return FindFrom(startPosition, key, mainRecordsCount)?.Record;
    }

    // Último registro da tabela na ordem da chave, inclusive se removido.
    public TRecord? GetLast()
    {
        var mainRecordsCount = GetMainRecordsCount();
        if (mainRecordsCount == 0)
            return null;

        return EnumerateFrom((mainRecordsCount - 1) * RecordSize, mainRecordsCount).Last().Record;
    }

    public void Insert(TRecord record)
    {
        var mainRecordsCount = GetMainRecordsCount();
        var newPosition = GetRecordsCount() * RecordSize;

        if (mainRecordsCount == 0)
        {
            WriteRecord(newPosition, record, flags: 0, EndOfTableContinuation);
            _fileStream.Flush();
            return;
        }

        MarkLastMainRecordAsEnd(mainRecordsCount);

        var previousIndex = FindLastMainRecord(mainRecordsCount, k => k.CompareTo(record.Key) <= 0);
        if (previousIndex < 0)
        {
            InsertBeforeFirst(record, newPosition, mainRecordsCount);
            return;
        }

        // Segue a cadeia de extensão do registro anterior até o último registro com chave menor ou igual à nova.
        var mainAreaEnd = mainRecordsCount * RecordSize;
        var previousPosition = previousIndex * RecordSize;
        var previous = PositionAndGetRecord(previousPosition);
        var nextPosition = GetNextPosition(previous, previousPosition, mainRecordsCount);

        while (nextPosition >= mainAreaEnd)
        {
            var next = PositionAndGetRecord(nextPosition.Value);
            if (next.Key.CompareTo(record.Key) > 0)
                break;

            previous = next;
            previousPosition = nextPosition.Value;
            nextPosition = GetNextPosition(previous, previousPosition, mainRecordsCount);
        }

        // O novo registro vai para o final do arquivo e aponta de volta para o próximo registro (ou para além do fim, se for o último).
        WriteRecord(newPosition, record, FlagsHelper.SetAsExtension(0), nextPosition ?? EndOfTableContinuation);
        WriteContinuation(previousPosition, newPosition);
        _fileStream.Flush();
    }

    public bool TryDelete(TKey key)
    {
        var found = TryFindRecord(key);
        if (found == null)
            return false;

        var (record, position) = found.Value;
        WriteBytes(position + FlagsOffset, [FlagsHelper.SetAsDeleted(record.Flags)]);
        _fileStream.Flush();
        return true;
    }

    // Substitui todo o conteúdo da tabela pelos registros informados, que devem estar ordenados pela chave.
    public long ReplaceAll(IEnumerable<TRecord> orderedRecords)
    {
        _fileStream.SetLength(0);
        _index = null;
        File.Delete(_indexFilePath);

        long count = 0;
        TRecord? previous = null;

        foreach (var record in orderedRecords)
        {
            if (previous != null && previous.Key.CompareTo(record.Key) > 0)
                throw new InvalidDataException("Os registros devem estar ordenados pela chave.");

            WriteRecord(count * RecordSize, record, flags: 0, continuation: 0);
            previous = record;
            count++;
        }

        if (count > 0)
            WriteContinuation((count - 1) * RecordSize, EndOfTableContinuation);

        _fileStream.Flush();
        return count;
    }

    protected abstract TRecord ParseRecord(ReadOnlySpan<byte> buffer);

    protected abstract TKey ParseKey(ReadOnlySpan<byte> buffer);

    // Grava os campos do registro. Flags, continuation e \n são preenchidos pela classe base.
    protected abstract void SerializeRecord(TRecord record, Span<byte> buffer);

    protected static void WriteFixedString(string value, Span<byte> buffer)
    {
        buffer.Clear();
        Encoding.ASCII.GetBytes(value.AsSpan(0, Math.Min(value.Length, buffer.Length)), buffer);
    }

    protected long GetRecordsCount()
        => _fileStream.Length / RecordSize;

    protected TRecord PositionAndGetRecord(long position)
    {
        _fileStream.Seek(position, SeekOrigin.Begin);
        if (_fileStream.Read(_sharedBuffer) != RecordSize)
            throw new InvalidDataException($"Could not load the specified record at position {position}.");

        return ParseRecord(_sharedBuffer);
    }

    // Percorre a tabela na ordem da chave a partir de um registro, seguindo os Continuations (inclui removidos).
    private IEnumerable<(TRecord Record, long Position)> EnumerateFrom(long position, long mainRecordsCount)
    {
        long? current = mainRecordsCount > 0 ? position : null;

        while (current != null)
        {
            var record = PositionAndGetRecord(current.Value);
            yield return (record, current.Value);

            current = GetNextPosition(record, current.Value, mainRecordsCount);
        }
    }

    // Posição do próximo registro na ordem da chave, ou null se este for o último da tabela.
    private long? GetNextPosition(TRecord record, long position, long mainRecordsCount)
    {
        if (record.Continuation == 0)
        {
            var nextPosition = position + RecordSize;
            return nextPosition < mainRecordsCount * RecordSize ? nextPosition : null;
        }

        if (record.Continuation > _fileStream.Length - RecordSize)
            return null;

        return record.Continuation;
    }

    private (TRecord Record, long Position)? TryFindRecord(TKey key)
    {
        var mainRecordsCount = GetMainRecordsCount();

        // Parte do último registro principal com chave menor: a cadeia de extensão dele pode conter a chave.
        var previousIndex = FindLastMainRecord(mainRecordsCount, k => k.CompareTo(key) < 0);
        return FindFrom(Math.Max(previousIndex, 0) * RecordSize, key, mainRecordsCount);
    }

    private (TRecord Record, long Position)? FindFrom(long position, TKey key, long mainRecordsCount)
    {
        foreach (var (record, recordPosition) in EnumerateFrom(position, mainRecordsCount))
        {
            var comparisonResult = key.CompareTo(record.Key);
            if (comparisonResult < 0)
                break;

            if (comparisonResult == 0 && !FlagsHelper.RecordIsDeleted(record))
                return (record, recordPosition);
        }

        return null;
    }

    // Busca binária na área principal: índice do último registro cuja chave satisfaz o predicado, ou -1.
    // O predicado deve ser verdadeiro para um prefixo da área principal (ex.: chave <= x).
    private long FindLastMainRecord(long mainRecordsCount, Func<TKey, bool> predicate)
    {
        long first = 0, last = mainRecordsCount;

        while (first < last)
        {
            var middle = first + (last - first) / 2;
            if (predicate(PositionAndGetRecord(middle * RecordSize).Key))
                first = middle + 1;
            else
                last = middle;
        }

        return first - 1;
    }

    // Os registros de extensão ficam todos no final, então a área principal termina no primeiro deles.
    private long GetMainRecordsCount()
    {
        long first = 0;
        long last = GetRecordsCount();

        while (first < last)
        {
            var middle = first + (last - first) / 2;
            if (FlagsHelper.RecordIsExtension(PositionAndGetRecord(middle * RecordSize)))
                last = middle;
            else
                first = middle + 1;
        }

        return first;
    }

    // Garante que o último registro da área principal não aponte "sequencialmente" para a área de extensão.
    private void MarkLastMainRecordAsEnd(long mainRecordsCount)
    {
        var position = (mainRecordsCount - 1) * RecordSize;
        if (PositionAndGetRecord(position).Continuation == 0)
            WriteContinuation(position, EndOfTableContinuation);
    }

    // Não há registro anterior para apontar ao novo: o primeiro registro é movido para o final do arquivo
    // e o novo ocupa a posição 0, apontando para ele. A área principal continua ordenada.
    private void InsertBeforeFirst(TRecord record, long newPosition, long mainRecordsCount)
    {
        var first = PositionAndGetRecord(0);
        var firstNextPosition = GetNextPosition(first, 0, mainRecordsCount);

        BinaryPrimitives.WriteInt64LittleEndian(_sharedBuffer.AsSpan(ContinuationOffset), firstNextPosition ?? EndOfTableContinuation);
        _sharedBuffer[FlagsOffset] = FlagsHelper.SetAsExtension(_sharedBuffer[FlagsOffset]);
        WriteBytes(newPosition, _sharedBuffer);

        WriteRecord(0, record, flags: 0, newPosition);
        _fileStream.Flush();
    }

    private void WriteRecord(long position, TRecord record, byte flags, long continuation)
    {
        SerializeRecord(record, _writeBuffer);
        _writeBuffer[FlagsOffset] = flags;
        BinaryPrimitives.WriteInt64LittleEndian(_writeBuffer.AsSpan(ContinuationOffset), continuation);
        _writeBuffer[^1] = (byte)'\n';

        WriteBytes(position, _writeBuffer);
    }

    private void WriteContinuation(long recordPosition, long continuation)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, continuation);
        WriteBytes(recordPosition + ContinuationOffset, buffer);
    }

    private void WriteBytes(long position, ReadOnlySpan<byte> bytes)
    {
        // Evita o Seek em gravações sequenciais, que descartaria o buffer do FileStream.
        if (_fileStream.Position != position)
            _fileStream.Seek(position, SeekOrigin.Begin);

        _fileStream.Write(bytes);
    }

    // O índice parcial guarda a chave e a posição de um a cada IndexInterval registros da área principal.
    // Como inserções não movem registros da área principal, ele só precisa ser refeito quando ela muda de tamanho.
    // Formato: quantidade de registros principais (long), intervalo (int), entradas (chave + posição).
    private List<IndexEntry> LoadIndex(long mainRecordsCount)
    {
        if (_index != null && _indexMainRecordsCount == mainRecordsCount)
            return _index;

        _index = TryReadIndexFile(mainRecordsCount) ?? BuildIndexFile(mainRecordsCount);
        _indexMainRecordsCount = mainRecordsCount;
        return _index;
    }

    private List<IndexEntry>? TryReadIndexFile(long mainRecordsCount)
    {
        if (!File.Exists(_indexFilePath))
            return null;

        using var stream = File.OpenRead(_indexFilePath);
        using var reader = new BinaryReader(stream);

        var entrySize = KeySize + sizeof(long);
        if (stream.Length < IndexHeaderSize || (stream.Length - IndexHeaderSize) % entrySize != 0)
            return null;

        if (reader.ReadInt64() != mainRecordsCount || reader.ReadInt32() != IndexInterval)
            return null;

        var entries = new List<IndexEntry>((int)((stream.Length - IndexHeaderSize) / entrySize));
        var keyBuffer = new byte[KeySize];

        while (stream.Position < stream.Length)
        {
            stream.ReadExactly(keyBuffer);
            entries.Add(new(ParseKey(keyBuffer), reader.ReadInt64()));
        }

        return entries;
    }

    private List<IndexEntry> BuildIndexFile(long mainRecordsCount)
    {
        var entries = new List<IndexEntry>();

        using var stream = File.Create(_indexFilePath);
        using var writer = new BinaryWriter(stream);

        writer.Write(mainRecordsCount);
        writer.Write(IndexInterval);

        for (long i = 0; i < mainRecordsCount; i += IndexInterval)
        {
            var position = i * RecordSize;
            var record = PositionAndGetRecord(position);

            writer.Write(_sharedBuffer, 0, KeySize);
            writer.Write(position);
            entries.Add(new(record.Key, position));
        }

        return entries;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || Disposed)
            return;

        _fileStream.Dispose();

        Disposed = true;
    }

    private sealed record IndexEntry(TKey Key, long Position);
}
