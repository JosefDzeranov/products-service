using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using StackExchange.Redis;
using ProductsService.Data;
using ProductsService.Models;
using ProductsService.Services;

var builder = WebApplication.CreateBuilder(args);

var connection = builder.Configuration.GetConnectionString("Postgres");

builder.Services.AddDbContext<ProductDbContext>(options => options.UseNpgsql(connection));

builder.Services.AddScoped<IProductRepository, EfProductRepository>();

// Redis как общий кэш. AbortOnConnectFail=false, чтобы сервис поднялся, даже если Redis
// ответит чуть позже, и переподключился сам, когда Redis станет доступен.
var redisConnection = builder.Configuration["Redis:Connection"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var options = ConfigurationOptions.Parse(redisConnection);
    options.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(options);
});

// Типизированный HTTP-клиент к сервису отзывов. Адрес приходит переменной окружения
// ReviewsService__Url, внутри compose это http://reviews-service:8080 (обращение по имени).
builder.Services.AddHttpClient<IReviewsClient, ReviewsClient>(client =>
{
    var url = builder.Configuration["ReviewsService:Url"]
        ?? throw new InvalidOperationException("Не задан адрес сервиса отзывов ReviewsService:Url");
    client.BaseAddress = new Uri(url);
});

builder.Services.AddControllers();

// Генерация OpenAPI-спеки, на ней строится визуальный интерфейс.
builder.Services.AddOpenApi();

var app = builder.Build();

// Приложение стоит за обратным прокси nginx. Читаем настоящий адрес и схему из
// заголовков X-Forwarded, которые шлет nginx. Без этого приложение думает, что его
// адрес это внутреннее имя products-service:8080, и Scalar подставляет его в запросы.
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownIPNetworks.Clear();   // доверяем прокси, нас фронтит только свой nginx
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await db.Database.EnsureCreatedAsync();

    // Немного товаров для демо, чтобы каталог не был пустым при первом запуске.
    if (!await db.Products.AnyAsync())
    {
        db.Products.AddRange(
            new Product { Name = "PRO C#. Основы программирования", Price = 2700 },
            new Product { Name = "PRO C#. Docker", Price = 9900 },
            new Product { Name = "PRO C#. Паттерны проектирования", Price = 19900 });
        await db.SaveChangesAsync();
    }
}

app.MapControllers();

// OpenAPI-спека на /openapi/v1.json и визуальный интерфейс Scalar на /scalar/v1.
app.MapOpenApi();
app.MapScalarApiReference();

// Простой healthcheck, удобно проверить что сервис жив.
app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();
