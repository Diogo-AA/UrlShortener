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

    public override bool Equals(object? obj)
    {
        if (obj is User user)
        {
            return this.Username == user.Username && this.Id == user.Id;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Username);
    }
}
