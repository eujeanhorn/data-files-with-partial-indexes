using System;

namespace ArquivosDeIndice
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string productDataPath = "products.bin";
            string productIndexPath = "products.idx";
            string sessionDataPath = "sessions.bin";
            string sessionIndexPath = "sessions.idx";

            IndexManager.BuildIndex(productDataPath, productIndexPath);
            SessionIndexManager.BuildIndex(sessionDataPath, sessionIndexPath);
        }
    }
}