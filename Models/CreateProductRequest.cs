namespace ProductsService.Models;

// То, что присылает клиент, когда добавляет товар.
public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
