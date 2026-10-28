using System;
using System.IO;
using System.Text;

namespace ArquivosDeIndice
{
    public class SessionActions
    {
        public static void InsertRecord(string filePath, SessionRecord record)
        {
            record.Flags = 0;
            record.Continuacao = -1;

            using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                fs.Seek(0, SeekOrigin.End);
                using (var bw = new BinaryWriter(fs, Encoding.ASCII))
                {
                    bw.Write(GetFixedSizeAsciiBytes(record.UserSession, 36));
                    bw.Write(record.UserId);
                    bw.Write(record.ProductId);
                    bw.Write(GetFixedSizeAsciiBytes(record.EventType, 8));
                    bw.Write(record.EventTime);
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