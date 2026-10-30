using ECommerceDatabase.Menus;
using ECommerceDatabase.Menus.Options;
using ECommerceDatabase.Products.Menus;
using ECommerceDatabase.Products.TableReaders;
using ECommerceDatabase.UserSessions.Menus;
using ECommerceDatabase.UserSessions.TableReaders;

namespace ECommerceDatabase;

public static class Program
{
    public static void Main()
    {
        using var productsTableReader = new ProductsTableReader("C:\\Users\\Jean Horn\\Desktop\\user_sessions.bin");
        using var userSessionTableReader = new UserSessionsTableReader("C:\\Users\\Jean Horn\\Desktop\\products.bin");

        var productsMenu = ProductsMenu.Create(productsTableReader);
        var userSessionsMenu = UserSessionsMenu.Create(userSessionTableReader);

        new Menu("Menu principal")
            .AddOption(1, "Produtos", () =>
            {
                productsMenu.Run();
                return new MenuOptionExecutionDetails(null);
            })
            .AddOption(2, "Sessões dos usuários", () =>
            {
                userSessionsMenu.Run();
                return new MenuOptionExecutionDetails(null);
            })
            .AddOption(0, "Sair", null)
            .Run();
    }
}
