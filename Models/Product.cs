using System.ComponentModel.DataAnnotations;

namespace UltraPokemon.Models;

public class Product
{
    public int Id { get; set; }

    //  Khôi phục chuỗi thông báo tiếng Việt chuẩn UTF-8
    [Required(ErrorMessage = "Tên thẻ bài không được để trống.")]
    [StringLength(100, ErrorMessage = "Tên thẻ bài tối đa 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn hệ nguyên tố.")]
    [StringLength(50)]
    public string ElementType { get; set; } = string.Empty;

    [Range(0, 10000, ErrorMessage = "Chỉ số sức mạnh phải từ 0 trở lên.")]
    public int Power { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá bán không được là số âm.")]
    public decimal Price { get; set; }

    public string Description { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không được âm.")]
    public int Stock { get; set; }  // Quản lý tồn kho và thanh toán
}