using System.Security.Cryptography.X509Certificates;
using UrlShortener.Handlers;
using UrlShortener.Policies.Handlers;
using UrlShortener.Policies.Validation;
using UrlShortener.Options;
using Microsoft.AspNetCore.DataProtection;
using UrlShortener.Data;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using UrlShortener.Services;

var builder = WebApplication.CreateBuilder(args);

bool useHttps = builder.Configuration.GetValue<bool>("USE_HTTPS");

if (useHttps)
{
    builder.WebHost.UseUrls(["http://+:80", "https://+:443"]);

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false;
        options.ConfigureHttpsDefaults(httpsOptions =>
        {
            httpsOptions.ServerCertificate = X509CertificateLoader.LoadPkcs12FromFile("/https/aspnetapp.pfx",
                                                builder.Configuration.GetValue<string?>("PASSWORD_HTTPS_CERTIFICATE"));
        });
    });

    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
        .ProtectKeysWithCertificate(X509CertificateLoader.LoadPkcs12FromFile("/app/keys/datacert.pfx",
                                                builder.Configuration.GetValue<string?>("PASSWORD_DATA_PROTECTION_CERTIFICATE")));
}
else
{
    builder.WebHost.UseUrls(["http://+:80"]);
    
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false;
    });
}

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddExceptionHandler<CustomExceptionHandler>();
}

builder.Services.AddScoped<IRepository, RepositoryPostgre>(sp =>
{
    string? postgresConnectionString = builder.Configuration["ConnectionStrings:PostgresConnection"];
    if (string.IsNullOrEmpty(postgresConnectionString))
        throw new ArgumentException("Error: Postgre connection string cannot be null or empty");
    return new RepositoryPostgre(postgresConnectionString);
});

builder.Services.AddStackExchangeRedisCache(options => 
{
    string? redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection");
    if (string.IsNullOrEmpty(redisConnectionString))
        throw new ArgumentException("Error: Redis connection string cannot be null or empty");
    options.Configuration = redisConnectionString;
});

builder.Services.AddScoped<IShortenerService, ShortenerService>();
builder.Services.AddScoped<IApiKeyValidation, ApiKeyValidation>();
builder.Services.AddScoped<IUserAndPasswordValidation, UserAndPasswordValidation>();

builder.Services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyHandler>(ApiKeyAuthenticationOptions.DefaultScheme, null)
    .AddScheme<UserAndPasswordAuthenticationOptions, UserAndPasswordHandler>(UserAndPasswordAuthenticationOptions.DefaultScheme, null);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(ApiKeyAuthenticationOptions.DefaultPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddAuthenticationSchemes(ApiKeyAuthenticationOptions.DefaultScheme);
    })
    .AddPolicy(UserAndPasswordAuthenticationOptions.DefaultPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddAuthenticationSchemes(UserAndPasswordAuthenticationOptions.DefaultScheme);
    });

builder.Services.AddRateLimiter(options => 
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext => 
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = RateLimitingOptions.PERMIT_LIMIT,
                QueueLimit = RateLimitingOptions.QUEUE_LIMIT,
                Window = TimeSpan.FromSeconds(RateLimitingOptions.WINDOW_SECONDS)
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.RetryAfter = RateLimitingOptions.WINDOW_SECONDS.ToString();

        await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", cancellationToken);
    };
});

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var repository = scope.ServiceProvider.GetRequiredService<IRepository>();
    repository.Initialize();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.MapHealthChecks("/health");

if (useHttps)
    app.UseHttpsRedirection();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();
