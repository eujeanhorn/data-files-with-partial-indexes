using ECommerceDatabase.TableReaders.Models;

namespace ECommerceDatabase.UserSessions.Domain;

internal class UserSession : ITableRecord<string>
{
    public string ID { get; }
    public int UserID { get; }
    public int ProductID { get; }
    public string EventType { get; }
    public DateTime EventTime { get; }
    public byte Flags { get; }
    public long Continuation { get; }

    public string Key => ID;

    public const int IdLength = 36;
    public const int EventTypeLength = 8;

    public UserSession(string iD, int userID, int productID, string eventType, DateTime eventTime, byte flags, long continuation)
    {
        ID = iD;
        UserID = userID;
        ProductID = productID;
        EventType = eventType;
        EventTime = eventTime;
        Flags = flags;
        Continuation = continuation;
    }
}
