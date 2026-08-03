using Microsoft.EntityFrameworkCore;
using ProductsService.Data;
using ProductsService.Models;

namespace ProductsService.Services;

public class EfProductRepository : IProductRepository
{
    private readonly ProductDbContext _db;

    public EfProductRepository(ProductDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        return await _db.Products
            .OrderBy(p => p.Id)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _db.Products.FindAsync(id);
    }

    public async Task AddAsync(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
    }
}
