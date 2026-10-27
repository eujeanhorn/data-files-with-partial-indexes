using System;
using System.Collections.Generic;
using System.IO;

namespace ArquivosDeIndice
{
    public class IndexEntry
    {
        public int ProductId { get; set; }
        public long ByteOffset { get; set; }
        public int RecordIndex { get; set; }
    }

    internal class IndexManager
    {
        private const int BlockSize = 50;
        private const int RecordSize = 100;

        public static void BuildIndex(string binFilePath, string indexFilePath)
        {
            var indexEntries = new List<IndexEntry>();

            if (!File.Exists(binFilePath)) return;

            using (var fs = new FileStream(binFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var br = new BinaryReader(fs))
            {
                int totalRecords = (int)(fs.Length / RecordSize);
                int validIndexCount = 0;

                for (int i = 0; i < totalRecords; i++)
                {
                    long currentPosition = fs.Position;

                    int productId = br.ReadInt32();

                    fs.Seek(currentPosition + 90, SeekOrigin.Begin);
                    byte flags = br.ReadByte();

                    fs.Position = currentPosition + RecordSize;

                    bool isDeleted = (flags & 1) != 0;
                    if (isDeleted) continue;

                    if (validIndexCount % BlockSize == 0)
                    {
                        indexEntries.Add(new IndexEntry
                        {
                            ProductId = productId,
                            ByteOffset = currentPosition,
                            RecordIndex = i
                        });
                    }

                    validIndexCount++;
                }
            }

            using (var fs = new FileStream(indexFilePath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(indexEntries.Count);
                foreach (var entry in indexEntries)
                {
                    bw.Write(entry.ProductId);
                    bw.Write(entry.ByteOffset);
                    bw.Write(entry.RecordIndex);
                }
            }
        }

        public static List<IndexEntry> ReadIndex(string indexFilePath)
        {
            var entries = new List<IndexEntry>();
            if (!File.Exists(indexFilePath)) return entries;

            using (var fs = new FileStream(indexFilePath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                int count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    entries.Add(new IndexEntry
                    {
                        ProductId = br.ReadInt32(),
                        ByteOffset = br.ReadInt64(),
                        RecordIndex = br.ReadInt32()
                    });
                }
            }

            return entries;
        }
    }
}