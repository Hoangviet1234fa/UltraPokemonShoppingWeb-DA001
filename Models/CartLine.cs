namespace UltraPokemon.Models;

public class CartLine
{
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal Total => Product.Price * Quantity;
}
