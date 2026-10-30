namespace ECommerceDatabase.Products.Models;

internal class ProductFormData
{
    public long CategoryID { get; internal set; }
    public string CategoryCode { get; internal set; } = string.Empty;
    public string Brand { get; internal set; } = string.Empty;
    public double Price { get; internal set; }
}
