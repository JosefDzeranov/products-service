using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ProductsService.Models;
using ProductsService.Services;
using StackExchange.Redis;

namespace ProductsService.Controllers;

[ApiController]
[Route("products")]
public class ProductsController : ControllerBase
{
    // Ключ, под которым в Redis лежит закэшированный список товаров.
    private const string CatalogCacheKey = "products:all";

    private readonly IProductRepository _repository;
    private readonly IReviewsClient _reviews;
    private readonly IConnectionMultiplexer _redis;

    public ProductsController(
        IProductRepository repository,
        IReviewsClient reviews,
        IConnectionMultiplexer redis)
    {
        _repository = repository;
        _reviews = reviews;
        _redis = redis;
    }

    // GET /products  весь каталог. Сначала смотрим в Redis, база нужна только при промахе.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetAll()
    {
        var cache = _redis.GetDatabase();

        var cached = await cache.StringGetAsync(CatalogCacheKey);
        if (cached.HasValue)
        {
            // Кэш-попадание. В базу не ходим, отдаем готовый список из Redis.
            var fromCache = JsonSerializer.Deserialize<List<Product>>((string)cached!);
            return Ok(fromCache);
        }

        // Кэш-промах. Читаем из базы и кладем результат в Redis на 30 секунд.
        var products = await _repository.GetAllAsync();
        await cache.StringSetAsync(CatalogCacheKey, JsonSerializer.Serialize(products), TimeSpan.FromSeconds(30));
        return Ok(products);
    }

    // GET /products/{id}  один товар вместе с отзывами. Отзывы берем из reviews-service по сети.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDetails>> GetById(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product is null)
            return NotFound();

        // Вызов другого сервиса по имени. Если отзывы недоступны, вернется пустой список.
        var reviews = await _reviews.GetForProductAsync(id.ToString());

        var details = new ProductDetails
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Reviews = reviews,
            ReviewsCount = reviews.Count,
            AverageRating = reviews.Count == 0
                ? 0
                : Math.Round(reviews.Average(r => (double)r.Rating), 2)
        };

        return Ok(details);
    }

    // POST /products  добавить товар.
    [HttpPost]
    public async Task<ActionResult<Product>> Create(CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Не указано название товара");

        if (request.Price < 0)
            return BadRequest("Цена не может быть отрицательной");

        var product = new Product
        {
            Name = request.Name,
            Price = request.Price
        };

        await _repository.AddAsync(product);

        // Каталог изменился, старый кэш больше не актуален. Сбрасываем.
        await _redis.GetDatabase().KeyDeleteAsync(CatalogCacheKey);

        return Ok(product);
    }
}
