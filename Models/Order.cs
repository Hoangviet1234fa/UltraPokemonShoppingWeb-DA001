using System.ComponentModel.DataAnnotations;

namespace UltraPokemon.Models
{
    // Cấu trúc Enum để định nghĩa các trạng thái đơn hàng.
    // Việc dùng Enum giúp code chuyên nghiệp, dễ quản lý trạng thái và không sợ gõ sai chính tả.
    public enum OrderStatus
    {
        Pending,   // Mặc định khi khách vừa đặt hàng: Chờ duyệt
        Shipping,  // Khi Admin bấm duyệt: Đang giao hàng
        Completed, // Hoàn thành
        Cancelled  // Đơn hàng bị hủy
    }

    public class Order
    {
        [Key] // Đánh dấu đây là Khóa chính (Primary Key) trong CSDL
        public int Id { get; set; }

        [Required] // Validation: Yêu cầu bắt buộc không được để trống trường này
        // Lưu email người đặt. Thêm "= null!" để báo cho .NET 8 biết biến này sẽ được gán dữ liệu hợp lệ sau, giúp tắt cảnh báo vàng (CS8618).
        public string UserEmail { get; set; } = null!;

        // Lưu thời điểm khách hàng chốt đơn
        public DateTime OrderDate { get; set; }

        // Lưu tổng tiền của hóa đơn
        public decimal TotalAmount { get; set; }

        // Trạng thái hiện tại của đơn hàng (Sử dụng cấu trúc Enum ở trên)
        public OrderStatus Status { get; set; }

        // Quan hệ 1 - Nhiều (1-N) trong Entity Framework: 
        // Một Hóa đơn (Order) có thể chứa nhiều Chi tiết món hàng (OrderDetail).
        // Khởi tạo sẵn "= new List<OrderDetail>()" để tắt cảnh báo vàng và tránh lỗi NullReferenceException khi truy xuất dữ liệu.
        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}