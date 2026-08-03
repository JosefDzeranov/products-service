namespace ProductsService.Models;

// Карточка товара, которую собирает products-service, товар плюс его отзывы из reviews-service.
public class ProductDetails
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public int ReviewsCount { get; set; }
    public double AverageRating { get; set; }

    public IReadOnlyList<ReviewDto> Reviews { get; set; } = [];
}
