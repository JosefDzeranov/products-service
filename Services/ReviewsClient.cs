using System.Net.Http.Json;
using ProductsService.Models;

namespace ProductsService.Services;

// Обертка над HTTP-вызовом сервиса отзывов. Знает только его адрес, не его базу.
public class ReviewsClient : IReviewsClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ReviewsClient> _logger;

    public ReviewsClient(HttpClient http, ILogger<ReviewsClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ReviewDto>> GetForProductAsync(string productId)
    {
        try
        {
            // reviews-service отдает отзывы продукта на GET /reviews/{productId}.
            var reviews = await _http.GetFromJsonAsync<List<ReviewDto>>($"/reviews/{productId}");
            return reviews ?? [];
        }
        catch (HttpRequestException ex)
        {
            // Сервис отзывов недоступен. Товар все равно показываем, просто без отзывов.
            _logger.LogWarning(ex, "Сервис отзывов недоступен, продукт {ProductId} вернем без отзывов", productId);
            return [];
        }
    }
}
