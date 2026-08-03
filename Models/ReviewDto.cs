namespace ProductsService.Models;

// Отзыв в том виде, в каком его отдает reviews-service. Берем только нужные поля.
public class ReviewDto
{
    public string Author { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Text { get; set; } = string.Empty;
}
