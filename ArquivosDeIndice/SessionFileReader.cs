using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ArquivosDeIndice
{
    internal class SessionFileReader
    {
        private const int RecordSize = 70;

        public static List<SessionRecord> ReadRecords(string filePath)
        {
            var records = new List<SessionRecord>();

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var br = new BinaryReader(fs, Encoding.ASCII))
            {
                while (fs.Position + RecordSize <= fs.Length)
                {
                    records.Add(new SessionRecord
                    {
                        UserSession = Encoding.ASCII.GetString(br.ReadBytes(36)).TrimEnd('\0', ' '),
                        UserId = br.ReadInt32(),
                        ProductId = br.ReadInt32(),
                        EventType = Encoding.ASCII.GetString(br.ReadBytes(8)).TrimEnd('\0', ' '),
                        EventTime = br.ReadInt64(),
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