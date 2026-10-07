using InvestingExile.Api;
using InvestingExile.Domain;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("InvestingExile")
    ?? Environment.GetEnvironmentVariable("INVESTINGEXILE_CONNECTION")
    ?? "Host=localhost;Port=5432;Database=investingexile;Username=investingexile;Password=investingexile";

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/items", async (AppDbContext db, HttpContext http, CancellationToken cancellationToken) =>
{
    http.Response.Headers.CacheControl = "no-store";
    return await LatestItemSnapshots.ListAsync(db, cancellationToken);
});

app.Run();

public partial class Program;
