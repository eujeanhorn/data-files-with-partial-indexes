using System.Globalization;

namespace ECommerceDatabase.Imports;

// Lê o CSV de eventos: event_time,event_type,product_id,category_id,category_code,brand,price,user_id,user_session
public static class EventsCsvReader
{
    public static IEnumerable<EventsCsvLine> ReadLines(string filePath)
    {
        foreach (var line in File.ReadLines(filePath).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = line.Split(',');

            yield return new EventsCsvLine(
                DateTime.ParseExact(columns[0], "yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
                columns[1],
                int.Parse(columns[2], CultureInfo.InvariantCulture),
                string.IsNullOrEmpty(columns[3]) ? 0 : long.Parse(columns[3], CultureInfo.InvariantCulture),
                columns[4],
                columns[5],
                string.IsNullOrEmpty(columns[6]) ? 0 : double.Parse(columns[6], CultureInfo.InvariantCulture),
                string.IsNullOrEmpty(columns[7]) ? 0 : int.Parse(columns[7], CultureInfo.InvariantCulture),
                columns[8]
            );
        }
    }
}

public sealed record EventsCsvLine(
    DateTime EventTime,
    string EventType,
    int ProductID,
    long CategoryID,
    string CategoryCode,
    string Brand,
    double Price,
    int UserID,
    string UserSession);
