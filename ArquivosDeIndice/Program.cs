using System;
using System.IO;

namespace ArquivosDeIndice
{
    class Program
    {
        static void Main(string[] args)
        {
            string productFile = "products.bin";
            string productIndex = "products.idx";

            if (File.Exists(productFile)) File.Delete(productFile);
            if (File.Exists(productIndex)) File.Delete(productIndex);

            var newProduct = new ProductRecord
            {
                ProductId = 1002,
                CategoryId = 88,
                CategoryCode = "livros",
                Brand = "EditoraXYZ",
                Price = 89.90
            };

            ProductActions.InsertRecord(productFile, newProduct);
            ProductIndexManager.BuildIndex(productFile, productIndex);

            var produtoEncontrado = ProductSearch.FindById(productFile, productIndex, 1002);
            if (produtoEncontrado != null)
            {
                Console.WriteLine($"Encontrado: ID={produtoEncontrado.ProductId}, Marca={produtoEncontrado.Brand}, Preço={produtoEncontrado.Price:C}");
            }
            else
            {
                Console.WriteLine("Não encontrado.");
            }


            string sessionFile = "sessions.bin";
            string sessionIndex = "sessions.idx";

            if (File.Exists(sessionFile)) File.Delete(sessionFile);
            if (File.Exists(sessionIndex)) File.Delete(sessionIndex);

            var newSession = new SessionRecord
            {
                UserSession = "sessao_abc123",
                UserId = 9988,
                ProductId = 1002,
                EventType = "view",
                EventTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            SessionActions.InsertRecord(sessionFile, newSession);
            SessionIndexManager.BuildIndex(sessionFile, sessionIndex);

            var sessaoEncontrada = SessionSearch.FindByUserSession(sessionFile, sessionIndex, "sessao_abc123");
            if (sessaoEncontrada != null)
            {
                Console.WriteLine($"Encontrada: UserSession={sessaoEncontrada.UserSession}, UserId={sessaoEncontrada.UserId}, Evento={sessaoEncontrada.EventType}");
            }
            else
            {
                Console.WriteLine("Não encontrada.");
            }
        }
    }
}