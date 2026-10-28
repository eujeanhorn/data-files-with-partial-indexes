using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ArquivosDeIndice
{
    internal class ProductFileReader
    {
        private const int RecordSize = 100;

        public static List<ProductRecord> ReadRecords(string filePath)
        {
            var records = new List<ProductRecord>();

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var br = new BinaryReader(fs, Encoding.ASCII))
            {
                while (fs.Position + RecordSize <= fs.Length)
                {
                    records.Add(new ProductRecord
                    {
                        ProductId = br.ReadInt32(),
                        CategoryId = br.ReadInt64(),
                        CategoryCode = Encoding.ASCII.GetString(br.ReadBytes(38)).TrimEnd('\0', ' '),
                        Brand = Encoding.ASCII.GetString(br.ReadBytes(32)).TrimEnd('\0', ' '),
                        Price = br.ReadDouble(),
                        Flags = br.ReadByte(),
                        Continuacao = br.ReadInt64()
                    });

                    br.ReadByte();
                }
            }

            return records;
        }
    }
}