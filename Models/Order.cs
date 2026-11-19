using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UltraPokemon.Models
{
   
    /// Enum định nghĩa các trạng thái xử lý của đơn hàng.
    /// Attribute [Display] giúp tự động hiển thị tên tiếng Việt trên giao diện Admin.
   
    public enum OrderStatus
    {
        [Display(Name = "Chờ duyệt")]
        Pending = 0,    // Đơn mới tạo, đang chờ Admin xác nhận

        [Display(Name = "Đang giao")]
        Shipping = 1,   // Đã đóng gói và bàn giao cho đơn vị vận chuyển

        [Display(Name = "Hoàn thành")]
        Completed = 2,  // Khách đã nhận hàng và hoàn tất giao dịch

        [Display(Name = "Đã hủy")]
        Cancelled = 3   // Đơn bị hủy (Hệ thống tự động hoàn trả tồn kho)
    }

    
    /// Entity ánh xạ tới bảng Orders trong cơ sở dữ liệu.
 
    public class Order
    {
        [Key] // Khóa chính (Primary Key), tự động tăng (Identity)
        public int Id { get; set; }

        [Required(ErrorMessage = "Email tài khoản đặt hàng không được để trống.")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
        [StringLength(150)] // Tối ưu kiểu cột nvarchar(150) thay vì nvarchar(max)
        [Display(Name = "Email khách hàng")]
        public string UserEmail { get; set; } = null!;

        [Display(Name = "Ngày đặt hàng")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")] // Định nghĩa chuẩn kiểu tiền tệ, tránh cảnh báo EF Core
        [Display(Name = "Tổng tiền")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Trạng thái")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

       
        // THÔNG TIN GIAO NHẬN & PHƯƠNG THỨC THANH TOÁN
       

        [Required(ErrorMessage = "Họ tên người nhận không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        [Display(Name = "Người nhận")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Địa chỉ giao hàng không được để trống.")]
        [StringLength(250, ErrorMessage = "Địa chỉ tối đa 250 ký tự.")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string Address { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự.")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = null!;

        [Required]
        [StringLength(50)]
        [Display(Name = "Hình thức thanh toán")]
        public string PaymentMethod { get; set; } = "Tiền mặt (COD)"; // Khớp với giá trị trong CheckoutViewModel

        // Bổ sung trường Ghi chú đơn hàng (cho phép null nếu khách không nhập)
        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        // THUỘC TÍNH TIỆN ÍCH & QUAN HỆ ENTITY FRAMEWORK
        

       
        /// Thuộc tính chỉ dùng để hiển thị mã đơn đẹp (VD: #ULTRA000012).
        ///  báo cho EF Core không tạo cột này trong bảng dữ liệu.
    
        [NotMapped]
        public string OrderCode => $"#ULTRA{Id:D6}";

       
        /// Quan hệ 1 - Nhiều (1-N): Một hóa đơn chứa danh sách nhiều chi tiết sản phẩm (OrderDetail).
        /// Khởi tạo sẵn List rỗng để tránh lỗi NullReferenceException khi truy xuất.
       
        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public bool IsPaid { get; set; } = false;
    }
}