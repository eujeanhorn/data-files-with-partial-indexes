using System;
using System.IO;
using System.Text;

namespace ArquivosDeIndice
{
    public class SessionSearch
    {
        private const int RecordSize = 70;

        public static SessionRecord FindByUserSession(string binFilePath, string indexFilePath, string targetUserSession)
        {
            var entries = SessionIndexManager.ReadIndex(indexFilePath);

            if (entries.Count == 0 || !File.Exists(binFilePath))
            {
                return null;
            }

            long startOffset = entries[0].ByteOffset;
            int startIndex = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Compare(entries[i].UserSession, targetUserSession) <= 0)
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

            // Abre o FileStream e inicializa o BinaryReader APÓS o Seek
            using (var fs = new FileStream(binFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Seek(startOffset, SeekOrigin.Begin);
                using (var br = new BinaryReader(fs, Encoding.ASCII))
                {
                    while (fs.Position < endOffset && fs.Position + RecordSize <= fs.Length)
                    {
                        string userSession = Encoding.ASCII.GetString(br.ReadBytes(36)).TrimEnd('\0', ' ');
                        int userId = br.ReadInt32();
                        int productId = br.ReadInt32();
                        string eventType = Encoding.ASCII.GetString(br.ReadBytes(8)).TrimEnd('\0', ' ');
                        long eventTime = br.ReadInt64();
                        byte flags = br.ReadByte();
                        long continuacao = br.ReadInt64();
                        br.ReadByte(); // Lê o '\n'

                        bool isDeleted = (flags & 1) != 0;

                        if (!isDeleted && userSession == targetUserSession)
                        {
                            return new SessionRecord
                            {
                                UserSession = userSession,
                                UserId = userId,
                                ProductId = productId,
                                EventType = eventType,
                                EventTime = eventTime,
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