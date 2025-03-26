namespace UrlShortener.Models;

public class ApiKey
{
    public const int MAX_MINUTES_BETWEEN_UPDATES = 3;
    private const int EXPIRATION_TIME_DAYS = 7;

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid Key { get; set; }
    public DateTime ExpirationDate { get; set; }

    public enum ValidationStatus
    {
        Valid,
        Invalid,
        Expired
    }

    public static DateTime GetExpirationDate()
    {
        return GetExpirationDate(DateTime.Now);
    }

    public static DateTime GetExpirationDate(DateTime date)
    {
        return date.AddDays(EXPIRATION_TIME_DAYS);
    }
}
