using System;

namespace ArquivosDeIndice
{
    public class SessionRecord
    {
        public string UserSession { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public long EventTime { get; set; }
        public byte Flags { get; set; }
        public long Continuacao { get; set; }
    }
}