using UrlShortener.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
