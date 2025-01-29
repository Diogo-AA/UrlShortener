namespace UrlShortener.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }

    public User(Guid id, string username)
    {
        Id = id;
        Username = username;
    }
}
