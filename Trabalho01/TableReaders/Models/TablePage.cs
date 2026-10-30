namespace ECommerceDatabase.TableReaders.Models;

public sealed record TablePage<TRecord>(IReadOnlyList<TRecord> Records);
