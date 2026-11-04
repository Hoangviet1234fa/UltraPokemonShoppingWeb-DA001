namespace UltraPokemon.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ElementType { get; set; } = string.Empty;
    public int Power { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;

    public int Stock { get; set; } // <--- Thêm dòng này để quản lý tồn kho và thanh toán
}