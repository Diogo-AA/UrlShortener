using Npgsql;
using UrlShortener.Models;
using UrlShortener.Utils;

namespace UrlShortener.Infrastructure;

public class DbController
{
    private readonly NpgsqlDataSource dataSource;
    public const int LIMIT_URLS_SHOWN = 100;
    public const int DEFAULT_URLS_SHOWN = 20;

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
            using var conn = await dataSource.OpenConnectionAsync();

            string sql = """
                SELECT id, username
                FROM "Users"
                WHERE username = @username
                LIMIT 1
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("username", username);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return null;

            await reader.ReadAsync();

            return new User 
            {
                Id = reader.GetGuid(0), 
                Username = reader.GetString(1)
            };
        }
        catch
        {
            throw;
        }
    }

    public async Task<User?> GetUser(Guid apiKey)
    {
        try
        {
            using var conn = await dataSource.OpenConnectionAsync();

            string sql = """
                SELECT id, username
                FROM "Users"
                INNER JOIN "ApiKeys" ON key = @apiKey
                LIMIT 1
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("apiKey", apiKey);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return null;

            await reader.ReadAsync();

            return new User
            {
                Id = reader.GetGuid(0),
                Username = reader.GetString(1)
            };
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
            using var conn = await dataSource.OpenConnectionAsync();

            string sql = """
                SELECT username
                FROM "Users"
                WHERE username = @username
                LIMIT 1
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

    public async Task<Guid?> CreateUser(User user)
    {
        using var conn = await dataSource.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            string sql = """
                INSERT INTO "Users" (id, username, password) 
                VALUES (@id, @username, @password)
                """;
            await using var cmd = new NpgsqlCommand(sql, conn, transaction);

            user.Id = Guid.CreateVersion7();
            string hashedPassword = Cryptography.HashPassword(user.Password!);

            cmd.Parameters.AddWithValue("id", user.Id);
            cmd.Parameters.AddWithValue("username", user.Username!);
            cmd.Parameters.AddWithValue("password", hashedPassword);

            await cmd.ExecuteNonQueryAsync();

            Guid? apiKey = await CreateApiKey(user.Id, conn, transaction);
            if (!apiKey.HasValue)
            {
                await transaction.RollbackAsync();
                return null;
            }

            await transaction.CommitAsync();
            return apiKey.Value;
        }
        catch
        {
            await transaction.RollbackAsync();
            return null;
        }
    }

    public async Task<bool> UpdateUserPassword(User user)
    {
        using var conn = await dataSource.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            string sql = """
                UPDATE "Users"
                SET password = @password
                WHERE username = @username
                """;
            await using var cmd = new NpgsqlCommand(sql, conn, transaction);

            string newHashedPassword = Cryptography.HashPassword(user.NewPassword!);

            cmd.Parameters.AddWithValue("username", user.Username!);
            cmd.Parameters.AddWithValue("password", newHashedPassword);

            int numRowsUpdated = await cmd.ExecuteNonQueryAsync();
            if (numRowsUpdated != 1)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> VerifyUser(User user)
    {
        try
        {
            using var conn = await dataSource.OpenConnectionAsync();

            string sql = """
                SELECT id, password
                FROM "Users"
                WHERE username = @username
                LIMIT 1
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("username", user.Username!);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows)
                return false;

            await reader.ReadAsync();
            user.Id = reader.GetGuid(0);
            string hashedPassword = reader.GetString(1);
            return Cryptography.VerifyPassword(user.Password!, hashedPassword);
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> RemoveUser(string username)
    {
        using var conn = await dataSource.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            string sql = """
                DELETE FROM "Users"
                WHERE username = @username
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("username", username);

            int rowsModified = await cmd.ExecuteNonQueryAsync();
            transaction.Commit();

            return rowsModified == 1;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    #endregion

    #region Api Key functions

    public async Task<Guid?> CreateApiKey(Guid userId, NpgsqlConnection conn, NpgsqlTransaction transaction)
    {
        try
        {
            string sql = """
                INSERT INTO "ApiKeys" (id, "userId", key, "expirationDate")
                VALUES (@id, @userId, @key, @expirationDate)
                """;
            await using var cmd = new NpgsqlCommand(sql, conn, transaction);

            Guid id = Guid.CreateVersion7();
            Guid apiKey = Guid.CreateVersion7();
            DateTime expirationDate = ApiKey.GetExpirationDate();

            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("key", apiKey);
            cmd.Parameters.AddWithValue("expirationDate", expirationDate);

            await cmd.ExecuteNonQueryAsync();
            return apiKey;
        }
        catch
        {
            return null;
        }
    }

    public async Task<DateTime?> GetLastTimeApiKeyUpdated(Guid userId)
    {
        try
        {
            using var conn = await dataSource.OpenConnectionAsync();

            string sql = """
                SELECT "lastUpdated"
                FROM "ApiKeys"
                WHERE "userId" = @userId
                LIMIT 1
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return null;

            await reader.ReadAsync();
            return reader.GetDateTime(0);
        }
        catch
        {
            throw;
        }
    }

    public async Task<Guid?> UpdateApiKey(Guid userId)
    {
        using var conn = await dataSource.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            string sql = """
                UPDATE "ApiKeys"
                SET key = @key, "expirationDate" = @expirationDate
                WHERE "userId" = @userId
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);

            var apiKey = Guid.CreateVersion7();
            var expirationDate = ApiKey.GetExpirationDate();

            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("key", apiKey);
            cmd.Parameters.AddWithValue("expirationDate", expirationDate);

            int rowsUpdated = await cmd.ExecuteNonQueryAsync();
            if (rowsUpdated != 1)
            {
                await transaction.RollbackAsync();
                return null;
            }

            await transaction.CommitAsync();
            return apiKey;
        }
        catch
        {
            await transaction.RollbackAsync();
            return null;
        }
    }

    public async Task<Guid?> GetApiKey(Guid userId)
    {
        try
        {
            using var conn = await dataSource.OpenConnectionAsync();

            string sql = """
                SELECT key, "expirationDate"
                FROM "ApiKeys"
                WHERE "userId" = @userId
                LIMIT 1
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return null;

            await reader.ReadAsync();
            var apiKey = reader.GetGuid(0);
            var expirationDate = reader.GetDateTime(1);

            return expirationDate >= DateTime.Now ? apiKey : null;
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> ValidateApiKey(Guid apiKey)
    {
        try
        {
            using var conn = await dataSource.OpenConnectionAsync();

            string sql = """
                SELECT "expirationDate"
                FROM "ApiKeys"
                WHERE key = @apiKey
                LIMIT 1
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("apiKey", apiKey);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return false;

            await reader.ReadAsync();
            var expirationDate = reader.GetDateTime(0);

            return expirationDate >= DateTime.Now;
        }
        catch
        {
            throw;
        }
    }

    #endregion

    #region Urls functions

    public async Task<string?> CreateShortedUrl(Guid apiKey, string originalUrl)
    {
        User? user = await GetUser(apiKey);
        if (user is null)
        {
            return null;
        }

        using var conn = await dataSource.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {

            string sql = """
                INSERT INTO "Urls" (id, "userId", "shortedUrl", "originalUrl")
                VALUES (@id, @userId, @shortedUrl, @originalUrl)
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);

            var id = Guid.CreateVersion7();
            string shortedUrl = Cryptography.GenerateShortUrl();

            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("userId", user.Id);
            cmd.Parameters.AddWithValue("shortedUrl", shortedUrl);
            cmd.Parameters.AddWithValue("originalUrl", originalUrl);

            await cmd.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return shortedUrl;
        }
        catch
        {
            await transaction.RollbackAsync();
            return null;
        }
    }

    public async Task<string?> GetOriginalUrl(Guid userId, string shortedUrlId)
    {
        try
        {
            using var conn = await dataSource.OpenConnectionAsync();

            string sql = """
                SELECT "originalUrl"
                FROM "Urls"
                WHERE "userId" = @userId and "shortedUrl" = @shortedUrl
                LIMIT 1
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("shortedUrl", shortedUrlId);

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

    public async Task<bool> RemoveUrl(Guid apiKey, string shortedUrlId)
    {
        User? user = await GetUser(apiKey);
        if (user is null)
        {
            return false;
        }

        using var conn = await dataSource.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            string sql = """
                DELETE FROM "Urls"
                WHERE "userId" = @userId and "shortedUrl" = @shortedUrl
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("userId", user.Id);
            cmd.Parameters.AddWithValue("shortedUrl", shortedUrlId);

            int numRowsAffected = await cmd.ExecuteNonQueryAsync();
            if (numRowsAffected != 1)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<IEnumerable<Url>> GetAllUrlsFromUser(Guid apiKey, int limit)
    {
        User? user = await GetUser(apiKey) ?? throw new Exception("Couldn't get the user through the API Key");

        try
        {
            using var conn = await dataSource.OpenConnectionAsync();

            string sql = """
                SELECT "shortedUrl", "originalUrl"
                FROM "Urls"
                WHERE "userId" = @userId
                LIMIT @limit
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", user.Id);
            cmd.Parameters.AddWithValue("limit", limit);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return Enumerable.Empty<Url>();

            var urls = new List<Url>();
            while (await reader.ReadAsync())
            {
                urls.Add(new Url()
                {
                    ShortedUrl = reader.GetString(0),
                    OriginalUrl = reader.GetString(1)
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
