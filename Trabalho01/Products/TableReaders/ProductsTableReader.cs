using ECommerceDatabase.Products.Domain;
using ECommerceDatabase.TableReaders;
using System.Buffers.Binary;
using System.Text;

namespace ECommerceDatabase.Products.TableReaders;

public class ProductsTableReader : TableReader<Product, int>
{
    protected override int KeySize => sizeof(int);
    protected override int IndexInterval => 50;

    public ProductsTableReader(string filePath) : base(CreateSharedBuffer(), filePath)
    {
    }

    protected override Product ParseRecord(ReadOnlySpan<byte> buffer)
    {
        var id = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        buffer = buffer[sizeof(int)..];
        var categoryID = BinaryPrimitives.ReadInt64LittleEndian(buffer);
        buffer = buffer[sizeof(long)..];

        var categoryCode = Encoding.ASCII.GetString(buffer[..Product.CategoryCodeLength]).TrimEnd('\0');
        buffer = buffer[Product.CategoryCodeLength..];
        var brand = Encoding.ASCII.GetString(buffer[..Product.BrandLength]).TrimEnd('\0');
        buffer = buffer[Product.BrandLength..];

        var price = BinaryPrimitives.ReadDoubleLittleEndian(buffer);
        buffer = buffer[sizeof(double)..];
        var flags = buffer[0];
        buffer = buffer[sizeof(byte)..];
        var continuation = BinaryPrimitives.ReadInt64LittleEndian(buffer);

        // \n é ignorado.

        return new Product(
            id,
            categoryID,
            categoryCode,
            brand,
            price,
            flags,
            continuation
        );
    }

    protected override int ParseKey(ReadOnlySpan<byte> buffer)
        => BinaryPrimitives.ReadInt32LittleEndian(buffer);

    protected override void SerializeRecord(Product record, Span<byte> buffer)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer, record.ID);
        buffer = buffer[sizeof(int)..];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, record.CategoryID);
        buffer = buffer[sizeof(long)..];

        WriteFixedString(record.CategoryCode, buffer[..Product.CategoryCodeLength]);
        buffer = buffer[Product.CategoryCodeLength..];
        WriteFixedString(record.Brand, buffer[..Product.BrandLength]);
        buffer = buffer[Product.BrandLength..];

        BinaryPrimitives.WriteDoubleLittleEndian(buffer, record.Price);
    }

    private static byte[] CreateSharedBuffer()
        => new byte[
            sizeof(int) + // product_id
            sizeof(long) + // category_id
            Product.CategoryCodeLength + // category_code
            Product.BrandLength + // brand
            sizeof(double) + // price
            1 + // flags
            sizeof(long) + // continuation
            1 /* \n */];
}
