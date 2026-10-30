using ECommerceDatabase.Consoles;
using ECommerceDatabase.Forms;
using ECommerceDatabase.Forms.Fields.Implementations;
using ECommerceDatabase.Imports;
using ECommerceDatabase.Menus;
using ECommerceDatabase.Products.Domain;
using ECommerceDatabase.Products.Models;
using ECommerceDatabase.Products.TableReaders;
using ECommerceDatabase.TeableViewers;

namespace ECommerceDatabase.Products.Menus;

public static class ProductsMenu
{
    public static Menu Create(ProductsTableReader productsTableReader)
    {
        var productForm = BuildProductForm();
        var productSearchForm = BuildProductSearchForm();
        var importForm = BuildImportForm();

        return new Menu("Produtos", '-')
            .AddOption(1, "Inserir dados iniciais", () =>
            {
                var importData = importForm.Run();
                return new(CreateInitialData(importData.FilePath, productsTableReader));
            })
            .AddOption(2, "Mostrar dados", () =>
            {
                ShowProducts(productsTableReader);
                return new(null);
            })
            .AddOption(3, "Pesquisa direto no arquivo de dados", () =>
            {
                var searchData = productSearchForm.Run();
                ShowSearchResult(productsTableReader.TryGetRecord(searchData.ProductID));
                return new(null);
            })
            .AddOption(4, "Pesquisa usando índice parcial", () =>
            {
                var searchData = productSearchForm.Run();
                ShowSearchResult(productsTableReader.TryGetRecordByIndex(searchData.ProductID));
                return new(null);
            })
            .AddOption(5, "Inserir registro", () =>
            {
                var productData = productForm.Run();
                var createdProduct = SaveNewProduct(productData, productsTableReader);
                return new($"Produto criado com ID {createdProduct.ID}.");
            })
            .AddOption(6, "Remover registro", () =>
            {
                var searchData = productSearchForm.Run();
                return productsTableReader.TryDelete(searchData.ProductID)
                    ? new("Produto removido.")
                    : new("Produto não encontrado.");
            })
            .AddOption(0, "Voltar", null);
    }

    private static string? CreateInitialData(string csvFilePath, ProductsTableReader productsTableReader)
    {
        if (string.IsNullOrEmpty(csvFilePath))
            return null;

        if (!File.Exists(csvFilePath))
            return "Arquivo não encontrado.";

        if (!ConsoleUtils.Confirm("Os dados atuais de produtos serão substituídos. Continuar?"))
            return null;

        Console.WriteLine("Importando...");

        // Cada produto aparece em vários eventos: mantém a primeira ocorrência.
        var products = new Dictionary<int, Product>();
        foreach (var line in EventsCsvReader.ReadLines(csvFilePath))
            products.TryAdd(line.ProductID, new Product(line.ProductID, line.CategoryID, line.CategoryCode, line.Brand, line.Price, 0, 0));

        var count = productsTableReader.ReplaceAll(products.Values.OrderBy(p => p.ID));
        return $"{count} produtos importados.";
    }

    private static Product SaveNewProduct(ProductFormData productData, ProductsTableReader productsTableReader)
    {
        var id = (productsTableReader.GetLast()?.ID ?? 0) + 1;
        var product = new Product(id, productData.CategoryID, productData.CategoryCode, productData.Brand, productData.Price, 0, 0);

        productsTableReader.Insert(product);
        return product;
    }

    private static void ShowSearchResult(Product? record)
    {
        Console.Clear();
        Console.WriteLine();

        if (record == null)
        {
            Console.WriteLine("Registro não encontrado.");
            Console.WriteLine("------------------------");
            Console.WriteLine();
            Console.WriteLine("Pressione qualquer tecla para continuar.");
            ConsoleUtils.WaitForAnyKey();
            return;
        }

        Console.WriteLine("Registro encontrado:");
        Console.WriteLine("--------------------");
        Console.WriteLine("- ID: {0}", record.ID);
        Console.WriteLine("- ID categoria: {0}", record.CategoryID);
        Console.WriteLine("- Categoria: {0}", record.CategoryCode);
        Console.WriteLine("- Marca: {0}", record.Brand);
        Console.WriteLine("- Preço: {0}", record.Price);
        Console.WriteLine();
        Console.WriteLine("Pressione qualquer tecla para continuar.");
        ConsoleUtils.WaitForAnyKey();
    }

    private static void ShowProducts(ProductsTableReader productsTableReader)
    {
        const int recordsPerPage = 10;
        var pages = productsTableReader.GetAllPaginated(recordsPerPage);

        new TableViewer<Product>()
            .AddColumn("ID", p => p.ID.ToString(), 10)
            .AddColumn("ID categoria", p => p.CategoryID.ToString(), 30)
            .AddColumn("Categoria", p => p.CategoryCode, 38)
            .AddColumn("Marca", p => p.Brand, 32)
            .AddColumn("Preço", p => p.Price.ToString(), 10)
            .Run(pages, recordsPerPage);
    }

    private static Form<ProductFormData> BuildProductForm()
        => Form.Create<ProductFormData>()
            .AddField(new LongFormField<ProductFormData>("Informe o ID da categoria do produto", p => p.CategoryID, minValue: 1))
            .AddField(new StringFormField<ProductFormData>("Informe o código da categoria do produto", p => p.CategoryCode, maxLength: Product.CategoryCodeLength, optional: true))
            .AddField(new StringFormField<ProductFormData>("Informe a marca", p => p.Brand, maxLength: Product.BrandLength))
            .AddField(new DoubleFormField<ProductFormData>("Informe o preço", p => p.Price, minValue: 0));

    private static Form<ProductSearchFormData> BuildProductSearchForm()
        => Form.Create<ProductSearchFormData>()
            .AddField(new IntFormField<ProductSearchFormData>("Informe o ID do produto", p => p.ProductID, minValue: 1, optional: true));

    private static Form<ImportFormData> BuildImportForm()
        => Form.Create<ImportFormData>()
            .AddField(new StringFormField<ImportFormData>("Informe o caminho do arquivo CSV", p => p.FilePath, optional: true));
}
