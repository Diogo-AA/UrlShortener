namespace UrlShortener.Models;

public class ApiKey
{
    private const int EXPIRATION_TIME_DAYS = 7;

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid Key { get; set; }
    public DateTime ExpirationDate { get; set; }

    public static DateTime GetExpirationDate()
    {
        return DateTime.Now.AddDays(EXPIRATION_TIME_DAYS);
    }
}
