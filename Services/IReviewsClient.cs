using ProductsService.Models;

namespace ProductsService.Services;

public interface IReviewsClient
{
    Task<IReadOnlyList<ReviewDto>> GetForProductAsync(string productId);
}
