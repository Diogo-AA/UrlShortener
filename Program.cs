using System.Security.Cryptography.X509Certificates;
using UrlShortener.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

string connectionStringKey;
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureHttpsDefaults(httpsOptions =>
        {
            httpsOptions.ServerCertificate = X509CertificateLoader.LoadPkcs12FromFile("/https/aspnetapp.pfx", null);
        });
    });

    connectionStringKey = "ConnectionStrings:DevConnection";
}
else
{
    connectionStringKey = "ConnectionStrings:ProdConnection";
}

builder.Services.AddSingleton<IRepository, RepositoryPostgre>(sp =>
{
    string? connectionString = builder.Configuration[connectionStringKey];
    if (string.IsNullOrEmpty(connectionString))
        throw new ArgumentException("Error: Connection string cannot be null or empty");
    return new RepositoryPostgre(connectionString);
});

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
