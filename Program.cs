using System.Security.Cryptography.X509Certificates;
using UrlShortener.Infrastructure;
using UrlShortener.Handlers;
using UrlShortener.Policies.Handlers;
using UrlShortener.Policies.Validation;
using UrlShortener.Policies.Options;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .ProtectKeysWithCertificate(X509CertificateLoader.LoadPkcs12FromFile("/https/aspnetapp.pfx", null)); //Temporary certificate for DataProtection
    // Using the same certificate for DataProtection and Https is not a good practice for Production!

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureHttpsDefaults(httpsOptions =>
        {
            httpsOptions.ServerCertificate = X509CertificateLoader.LoadPkcs12FromFile("/https/aspnetapp.pfx", null);
        });
    });
}
else
{
    builder.Services.AddExceptionHandler<CustomExceptionHandler>();
}

builder.Services.AddScoped<IRepository, RepositoryPostgre>(sp =>
{
    string? connectionString = builder.Configuration["ConnectionStrings:PostgresConnection"];
    if (string.IsNullOrEmpty(connectionString))
        throw new ArgumentException("Error: Connection string cannot be null or empty");
    return new RepositoryPostgre(connectionString);
});

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
}

app.MapHealthChecks("/health");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
