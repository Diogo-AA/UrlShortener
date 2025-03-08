using Npgsql;
using UrlShortener.Models;
using UrlShortener.Utils;

namespace UrlShortener.Infrastructure;

public class DbController
{
    private readonly NpgsqlDataSource dataSource;

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

    public async Task<bool> CreateUser(string username, string password)
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

            var id = Guid.CreateVersion7();
            string hashedPassword = Cryptography.HashPassword(password);

            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("username", username);
            cmd.Parameters.AddWithValue("password", hashedPassword);

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
            using var conn = await dataSource.OpenConnectionAsync();

            string sql = """
                SELECT password
                FROM "Users"
                WHERE username = @username
                LIMIT 1
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

    public async Task<bool> RemoveUser(Guid id)
    {
        using var conn = await dataSource.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            string sql = """
                DELETE FROM "Users"
                WHERE id = @id
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);

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

    public async Task<Guid?> CreateApiKey(Guid userId)
    {
        using var conn = await dataSource.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            string sql = """
                INSERT INTO "ApiKeys" (id, "userId", key, "expirationDate")
                VALUES (@id, @userId, @key, @expirationDate)
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);

            Guid id = Guid.CreateVersion7();
            Guid apiKey = Guid.CreateVersion7();
            DateTime expirationDate = ApiKey.GetExpirationDate();

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

    #endregion

    #region Urls functions

    public async Task<string?> CreateShortedUrl(Guid userId, string originalUrl)
    {
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
            cmd.Parameters.AddWithValue("userId", userId);
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

    public async Task<bool> RemoveUrl(Guid userId, string shortedUrlId)
    {
        using var conn = await dataSource.OpenConnectionAsync();
        using var transaction = await conn.BeginTransactionAsync();

        try
        {
            string sql = """
                DELETE FROM "Urls"
                WHERE "userId" = @userId and "shortedUrl" = @shortedUrl
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("userId", userId);
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

    public async Task<IEnumerable<Url>> GetAllUrlsFromUser(Guid userId)
    {
        try
        {
            using var conn = await dataSource.OpenConnectionAsync();

            string sql = """
                SELECT id, "shortedUrl", "originalUrl"
                FROM "Urls"
                WHERE "userId" = @userId
                """;
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return Enumerable.Empty<Url>();

            var urls = new List<Url>();
            while (await reader.ReadAsync())
            {
                urls.Add(new Url()
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
