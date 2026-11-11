using System.ComponentModel.DataAnnotations;

namespace UltraPokemon.Models
{
    // Cấu trúc Enum để định nghĩa các trạng thái đơn hàng.
    // Việc dùng Enum giúp code chuyên nghiệp, dễ quản lý trạng thái và không sợ gõ sai chính tả.
    public enum OrderStatus
    {
        Pending = 0,    // Chờ duyệt
        Shipping = 1,   // Đang giao
        Completed = 2,  // Hoàn thành
        Cancelled = 3   // Đã hủy (Kích hoạt hoàn trả kho tự động)
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


        // CÁC TRƯỜNG THÔNG TIN MỚI THÊM ĐỂ ĐÁP ỨNG FORM THANH TOÁN


        [Required(ErrorMessage = "Họ tên người nhận không được để trống")]
        [StringLength(100)] // Giới hạn độ dài để tối ưu CSDL
        public string FullName { get; set; } = null!; // Họ tên người nhận hàng

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public string Address { get; set; } = null!; // Địa chỉ giao hàng chi tiết

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [StringLength(15)]
        public string PhoneNumber { get; set; } = null!; // Số điện thoại liên hệ

        [Required]
        public string PaymentMethod { get; set; } = "Tiền mặt"; // Phương thức thanh toán (Momo, Ngân hàng, Visa...)



        // Quan hệ 1 - Nhiều (1-N) trong Entity Framework: 
        // Một Hóa đơn (Order) có thể chứa nhiều Chi tiết món hàng (OrderDetail).
        // Khởi tạo sẵn "= new List<OrderDetail>()" để tắt cảnh báo vàng và tránh lỗi NullReferenceException khi truy xuất dữ liệu.
        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}