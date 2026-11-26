using System.ComponentModel.DataAnnotations;

namespace UltraPokemon.Models;


/// Entity ánh xạ tới bảng Reviews trong cơ sở dữ liệu.
/// Lưu trữ đánh giá của khách hàng sau khi đơn hàng đã giao thành công (OrderStatus.Completed).

public class Review
{
    [Key] // Khóa chính tự động tăng
    public int Id { get; set; }

    // Khóa ngoại liên kết tới sản phẩm (thẻ bài) được đánh giá
    [Required]
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    // Khóa ngoại liên kết tới đơn hàng đã mua (xác thực người mua thực tế)
    [Required]
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    // Tên khách hàng hiển thị trên trang chi tiết sản phẩm
    [Required]
    [StringLength(100)]
    public string ReviewerName { get; set; } = string.Empty;

    // Email tài khoản đã mua hàng
    [Required]
    [StringLength(150)]
    public string ReviewerEmail { get; set; } = string.Empty;

    // Số sao đánh giá từ 1 đến 5
    [Range(1, 5, ErrorMessage = "Vui lòng chọn số sao đánh giá từ 1 đến 5.")]
    public int Rating { get; set; }

    // Nội dung nhận xét chi tiết của khách hàng
    [Required(ErrorMessage = "Vui lòng nhập nội dung nhận xét.")]
    [StringLength(500, ErrorMessage = "Nội dung nhận xét tối đa 500 ký tự.")]
    public string Comment { get; set; } = string.Empty;

    // Thời điểm gửi đánh giá
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}