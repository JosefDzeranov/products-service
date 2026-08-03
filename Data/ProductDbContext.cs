using Microsoft.EntityFrameworkCore;
using ProductsService.Models;

namespace ProductsService.Data;

// Контекст базы данных. Одна таблица товаров.
public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(200);
            entity.Property(p => p.Price).HasPrecision(10, 2);
        });
    }
}
