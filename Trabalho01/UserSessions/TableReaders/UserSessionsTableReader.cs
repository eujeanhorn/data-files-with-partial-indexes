using ECommerceDatabase.TableReaders;
using ECommerceDatabase.UserSessions.Domain;
using System.Buffers.Binary;
using System.Text;

namespace ECommerceDatabase.UserSessions.TableReaders;

internal class UserSessionsTableReader : TableReader<UserSession, string>
{
    protected override int KeySize => UserSession.IdLength;
    protected override int IndexInterval => 1000;

    public UserSessionsTableReader(string filePath) : base(BuildSharedBuffer(), filePath)
    {
    }

    protected override UserSession ParseRecord(ReadOnlySpan<byte> buffer)
    {
        var id = ParseKey(buffer);
        buffer = buffer[UserSession.IdLength..];

        var userID = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        buffer = buffer[sizeof(int)..];

        var productID = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        buffer = buffer[sizeof(int)..];

        var eventType = Encoding.ASCII.GetString(buffer[..UserSession.EventTypeLength]).TrimEnd('\0');
        buffer = buffer[UserSession.EventTypeLength..];

        var eventTime = BinaryPrimitives.ReadInt64LittleEndian(buffer);
        buffer = buffer[sizeof(long)..];

        var flags = buffer[0];
        buffer = buffer[sizeof(byte)..];

        var continuation = BinaryPrimitives.ReadInt64LittleEndian(buffer);

        // \n é ignorado.

        return new UserSession(
            id,
            userID,
            productID,
            eventType,
            DateTime.FromBinary(eventTime),
            flags,
            continuation
        );
    }

    protected override string ParseKey(ReadOnlySpan<byte> buffer)
        => Encoding.ASCII.GetString(buffer[..UserSession.IdLength]).TrimEnd('\0');

    protected override void SerializeRecord(UserSession record, Span<byte> buffer)
    {
        WriteFixedString(record.ID, buffer[..UserSession.IdLength]);
        buffer = buffer[UserSession.IdLength..];

        BinaryPrimitives.WriteInt32LittleEndian(buffer, record.UserID);
        buffer = buffer[sizeof(int)..];

        BinaryPrimitives.WriteInt32LittleEndian(buffer, record.ProductID);
        buffer = buffer[sizeof(int)..];

        WriteFixedString(record.EventType, buffer[..UserSession.EventTypeLength]);
        buffer = buffer[UserSession.EventTypeLength..];

        BinaryPrimitives.WriteInt64LittleEndian(buffer, record.EventTime.ToBinary());
    }

    private static byte[] BuildSharedBuffer()
    {
        return new byte[
            UserSession.IdLength + // user_session
            sizeof(int) + // user_id
            sizeof(int) + // product_id
            UserSession.EventTypeLength + // event_type
            sizeof(long) + // event_time
            1 + // flags
            sizeof(long) + // continuation
            1 /* \n */];
    }
}
