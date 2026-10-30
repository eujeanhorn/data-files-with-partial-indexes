using ECommerceDatabase.Flags;
using ECommerceDatabase.TableReaders.Models;

namespace ECommerceDatabase.Products.Domain;

public class Product : ITableRecord<int>
{
    public int ID { get; }
    public long CategoryID { get; }
    public string CategoryCode { get; }
    public string Brand { get; }
    public double Price { get; }
    public byte Flags { get; private set; }
    public long Continuation { get; }

    public int Key => ID;

    public const int CategoryCodeLength = 38;
    public const int BrandLength = 32;

    public Product(int iD, long categoryID, string categoryCode, string brand, double price, byte flags, long continuation)
    {
        ID = iD;
        CategoryID = categoryID;
        CategoryCode = categoryCode;
        Brand = brand;
        Price = price;
        Flags = flags;
        Continuation = continuation;
    }

    public void SetAsDeleted()
        => Flags = FlagsHelper.SetAsDeleted(Flags);
}
