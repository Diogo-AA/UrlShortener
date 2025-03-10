using System.Security.Cryptography.X509Certificates;
using UrlShortener.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ServerCertificate = X509CertificateLoader.LoadPkcs12FromFile("/https/aspnetapp.pfx", null);
    });
});

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<DbController>(sp =>
{
    string? connectionString = sp.GetService<IConfiguration>()?["ConnectionString"];
    if (string.IsNullOrEmpty(connectionString))
        throw new ArgumentException("Error: Connection string cannot be null or empty");
    return new DbController(connectionString);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
