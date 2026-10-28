using System;
using System.IO;
using System.Text;

namespace ArquivosDeIndice
{
    public class ProductActions
    {
        public static void InsertRecord(string filePath, ProductRecord record)
        {
            record.Flags = 0;
            record.Continuacao = -1;

            using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                fs.Seek(0, SeekOrigin.End);
                using (var bw = new BinaryWriter(fs, Encoding.ASCII))
                {
                    bw.Write(record.ProductId);
                    bw.Write(record.CategoryId);
                    bw.Write(GetFixedSizeAsciiBytes(record.CategoryCode, 38));
                    bw.Write(GetFixedSizeAsciiBytes(record.Brand, 32));
                    bw.Write(record.Price);
                    bw.Write(record.Flags);
                    bw.Write(record.Continuacao);
                    bw.Write((byte)'\n');
                }
            }
        }

        private static byte[] GetFixedSizeAsciiBytes(string text, int targetLength)
        {
            if (string.IsNullOrEmpty(text))
                text = string.Empty;

            if (text.Length > targetLength)
                text = text.Substring(0, targetLength);
            else
                text = text.PadRight(targetLength, '\0');

            return Encoding.ASCII.GetBytes(text);
        }
    }
}