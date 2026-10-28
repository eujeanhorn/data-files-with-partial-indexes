using System;
using System.IO;
using System.Text;

namespace ArquivosDeIndice
{
    public class ProductSearch
    {
        private const int RecordSize = 100;

        public static ProductRecord FindById(string binFilePath, string indexFilePath, int targetProductId)
        {
            var entries = ProductIndexManager.ReadIndex(indexFilePath);

            if (entries.Count == 0 || !File.Exists(binFilePath))
            {
                return null;
            }

            long startOffset = entries[0].ByteOffset;
            int startIndex = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].ProductId <= targetProductId)
                {
                    startOffset = entries[i].ByteOffset;
                    startIndex = i;
                }
                else
                {
                    break;
                }
            }

            long endOffset = new FileInfo(binFilePath).Length;
            if (startIndex + 1 < entries.Count)
            {
                endOffset = entries[startIndex + 1].ByteOffset;
            }

            using (var fs = new FileStream(binFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Seek(startOffset, SeekOrigin.Begin);
                using (var br = new BinaryReader(fs, Encoding.ASCII))
                {
                    while (fs.Position < endOffset && fs.Position + RecordSize <= fs.Length)
                    {
                        int productId = br.ReadInt32();
                        long categoryId = br.ReadInt64();
                        string categoryCode = Encoding.ASCII.GetString(br.ReadBytes(38)).TrimEnd('\0', ' ');
                        string brand = Encoding.ASCII.GetString(br.ReadBytes(32)).TrimEnd('\0', ' ');
                        double price = br.ReadDouble();
                        byte flags = br.ReadByte();
                        long continuacao = br.ReadInt64();
                        br.ReadByte();

                        bool isDeleted = (flags & 1) != 0;

                        if (!isDeleted && productId == targetProductId)
                        {
                            return new ProductRecord
                            {
                                ProductId = productId,
                                CategoryId = categoryId,
                                CategoryCode = categoryCode,
                                Brand = brand,
                                Price = price,
                                Flags = flags,
                                Continuacao = continuacao
                            };
                        }
                    }
                }
            }

            return null;
        }
    }
}