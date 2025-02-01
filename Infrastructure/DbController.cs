using Npgsql;
using UrlShortener.Models;
using UrlShortener.Utils;

namespace UrlShortener.Infrastructure;

public class DbController
{
    private readonly NpgsqlDataSource? dataSource;

    public DbController(string connectionString)
    {
        dataSource = new NpgsqlDataSourceBuilder(connectionString)
                         .Build();

        if (dataSource is null)
            throw new Exception("Error: Couldn't connect to the database");
    }

    #region Users functions

    public async Task<User?> GetUser(string username)
    {
        try
        {
            using var conn = await dataSource!.OpenConnectionAsync();

            string sql = """
                SELECT TOP 1 id, username
                FROM users
                WHERE username = @username
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("username", username);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return null;

            await reader.ReadAsync();
            return new User(reader.GetGuid(0), reader.GetString(1));
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> IsUsernameInUse(string username)
    {
        try
        {
            using var conn = await dataSource!.OpenConnectionAsync();

            string sql = """
                SELECT TOP 1 username
                FROM users
                WHERE username = @username
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("username", username);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.HasRows;
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> CreateUser(string username, string password)
    {
        using var conn = await dataSource!.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            string sql = """
                INSERT INTO users (id, username, password) 
                VALUES (@id, @username, @password)
                """;
            await using var cmd = new NpgsqlCommand(sql, conn, transaction);

            var id = Guid.NewGuid();
            string passwordHashed = Cryptography.HashPassword(password);

            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("username", username);
            cmd.Parameters.AddWithValue("password", passwordHashed);

            await cmd.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> VerifyUser(string username, string password)
    {
        try
        {
            using var conn = await dataSource!.OpenConnectionAsync();

            string sql = """
                SELECT TOP 1 password
                FROM users
                WHERE username = @username
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("username", username);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows)
                return false;

            await reader.ReadAsync();
            string hashedPassword = reader.GetString(0);
            return Cryptography.VerifyPassword(password, hashedPassword);
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> RemoveUser(Guid id)
    {
        using var conn = await dataSource!.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            string sql = """
                DELETE FROM users
                WHERE id = @id
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);

            await cmd.ExecuteNonQueryAsync();
            transaction.Commit();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    #endregion

    #region Api Key functions

    public async Task<Guid?> CreateApiKey(string userId)
    {
        using var conn = await dataSource!.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            string sql = """
                INSERT INTO ApiKeys (id, userId, key, expirationDate) 
                VALUES (@id, @userId, @key, @expirationDate)
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);

            var id = Guid.NewGuid();
            var apiKey = Guid.NewGuid();
            var expirationDate = ApiKey.GetExpirationDate();

            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("key", apiKey);
            cmd.Parameters.AddWithValue("expirationDate", expirationDate);

            await cmd.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return apiKey;
        }
        catch
        {
            await transaction.RollbackAsync();
            return null;
        }
    }

    public async Task<Guid?> UpdateApiKey(string userId)
    {
        using var conn = await dataSource!.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            string sql = """
                UPDATE ApiKeys
                SET key = @key, expirationDate = @expirationDate
                WHERE userId = @userId
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);

            var apiKey = Guid.NewGuid();
            var expirationDate = ApiKey.GetExpirationDate();

            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("key", apiKey);
            cmd.Parameters.AddWithValue("expirationDate", expirationDate);

            await cmd.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return apiKey;
        }
        catch
        {
            await transaction.RollbackAsync();
            return null;
        }
    }

    public async Task<Guid?> GetApiKey(string userId)
    {
        try
        {
            using var conn = await dataSource!.OpenConnectionAsync();

            string sql = """
                SELECT TOP 1 key, expirationDate
                FROM ApiKeys
                WHERE userId = @userId
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows)
                return null;

            await reader.ReadAsync();
            var apiKey = reader.GetGuid(0);
            var expirationDate = reader.GetDateTime(1);

            return expirationDate < DateTime.Now ? apiKey : null;
        }
        catch
        {
            throw;
        }
    }

    #endregion

    #region Urls functions

    public async Task<bool> CreateUrl(Guid userId, string originalUrl)
    {
        using var conn = await dataSource!.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            string sql = """
                INSERT INTO urls (id, userId, shortedUrl, originalUrl)
                VALUES (@id, @userId, @shortedUrl, @originalUrl)
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);

            var id = Guid.NewGuid();
            string shortedUrl = Cryptography.CreateShortedUrl(originalUrl);

            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("shortedUrl", shortedUrl);
            cmd.Parameters.AddWithValue("originalUrl", originalUrl);

            await cmd.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<string?> GetOriginalUrl(Guid userId, string shortedUrl)
    {
        try
        {
            using var conn = await dataSource!.OpenConnectionAsync();

            string sql = """
                SELECT TOP 1 originalUrl
                FROM urls
                WHERE userId = @userId and shortedUrl = @shortedUrl
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("shortedUrl", shortedUrl);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return null;

            await reader.ReadAsync();
            return reader.GetString(0);
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> RemoveUrl(Guid userId, string originalUrl)
    {
        using var conn = await dataSource!.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            string sql = """
                DELETE FROM urls
                WHERE userId = @userId and originalUrl = @originalUrl
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("originalUrl", originalUrl);

            await cmd.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<List<UrlShorted>> GetAllUrlsFromUser(Guid userId)
    {
        var urls = new List<UrlShorted>();
        try
        {
            using var conn = await dataSource!.OpenConnectionAsync();

            string sql = """
                SELECT id, shortedUrl, originalUrl
                FROM urls
                WHERE userId = @userId
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                urls.Add(new UrlShorted()
                {
                    Id = reader.GetGuid(0),
                    UserId = userId,
                    ShortedUrl = reader.GetString(1),
                    OriginalUrl = reader.GetString(2)
                });
            }

            return urls;
        }
        catch
        {
            throw;
        }
    }

    #endregion
}
