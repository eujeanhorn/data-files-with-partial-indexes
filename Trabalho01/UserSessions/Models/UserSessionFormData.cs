namespace ECommerceDatabase.UserSessions.Models;

internal class UserSessionFormData
{
    public int UserID { get; set; }
    public int ProductID { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
}
