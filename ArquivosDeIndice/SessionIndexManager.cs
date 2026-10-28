using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ArquivosDeIndice
{
    public class SessionIndexEntry
    {
        public string UserSession { get; set; } = string.Empty;
        public int UserId { get; set; }
        public long ByteOffset { get; set; }
        public int RecordIndex { get; set; }
    }

    internal class SessionIndexManager
    {
        private const int BlockSize = 50;
        private const int RecordSize = 70;

        public static void BuildIndex(string binFilePath, string indexFilePath)
        {
            var indexEntries = new List<SessionIndexEntry>();

            if (!File.Exists(binFilePath)) return;

            using (var fs = new FileStream(binFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var br = new BinaryReader(fs, Encoding.ASCII))
            {
                int totalRecords = (int)(fs.Length / RecordSize);
                int validIndexCount = 0;

                for (int i = 0; i < totalRecords; i++)
                {
                    long currentPosition = fs.Position;

                    // Leitura sequencial exata dos 70 bytes do registro de sessão
                    string userSession = Encoding.ASCII.GetString(br.ReadBytes(36)).TrimEnd('\0', ' ');
                    int userId = br.ReadInt32();
                    br.ReadInt32();          // ProductId
                    br.ReadBytes(8);         // EventType
                    br.ReadInt64();          // EventTime
                    byte flags = br.ReadByte(); // Flags
                    br.ReadInt64();          // Continuacao
                    br.ReadByte();           // '\n'

                    bool isDeleted = (flags & 1) != 0;
                    if (isDeleted) continue;

                    if (validIndexCount % BlockSize == 0)
                    {
                        indexEntries.Add(new SessionIndexEntry
                        {
                            UserSession = userSession,
                            UserId = userId,
                            ByteOffset = currentPosition,
                            RecordIndex = i
                        });
                    }

                    validIndexCount++;
                }
            }

            // Garante que o índice fique ordenado alfabeticamente por UserSession
            indexEntries = indexEntries.OrderBy(e => e.UserSession).ToList();

            using (var fs = new FileStream(indexFilePath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(indexEntries.Count);
                foreach (var entry in indexEntries)
                {
                    bw.Write(entry.UserSession);
                    bw.Write(entry.UserId);
                    bw.Write(entry.ByteOffset);
                    bw.Write(entry.RecordIndex);
                }
            }
        }

        public static List<SessionIndexEntry> ReadIndex(string indexFilePath)
        {
            var entries = new List<SessionIndexEntry>();
            if (!File.Exists(indexFilePath)) return entries;

            using (var fs = new FileStream(indexFilePath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                int count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    entries.Add(new SessionIndexEntry
                    {
                        UserSession = br.ReadString(),
                        UserId = br.ReadInt32(),
                        ByteOffset = br.ReadInt64(),
                        RecordIndex = br.ReadInt32()
                    });
                }
            }

            return entries;
        }
    }
}